using Microsoft.AspNetCore.Mvc;
using testweb.Data;
using testweb.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;        // 👈 這是為了讓 Task 運作
using Microsoft.AspNetCore.SignalR;
using testweb.Hubs;
using System.Net;

namespace testweb.Controllers;

public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<OrderHub> _hubContext;

    // 透過建構子把資料庫接進來
    public OrderController(ApplicationDbContext db, IHubContext<OrderHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    // 1. 搜尋 API
    [HttpPost]
    public JsonResult SearchOrders(string orderId, string content)
    {
        // 如果搜尋「訂單內容」的 input 還留著，可以維持搜尋商品的邏輯，但回傳不顯示
        var query = _db.Orders.Include(o => o.OrderItems).AsQueryable();

        if (!string.IsNullOrEmpty(orderId))
        {
            query = query.Where(o => o.OrderNumber.Contains(orderId));
        }

        if (!string.IsNullOrEmpty(content))
        {
            // 雖然畫面上不顯示內容，但使用者輸入關鍵字時，我們依然去比對商品名稱
            query = query.Where(o => o.OrderItems.Any(item => item.ProductName.Contains(content)));
        }

        var results = query.Select(o => new
        {
            orderNumber = o.OrderNumber,
            createTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm"),
            status = o.Status
        }).ToList();

        return Json(results);
    }

    // 2. 初始化測試資料 (網址輸入 /Order/InitTestData 觸發)
    public IActionResult InitTestData()
    {
        if (!_db.Orders.Any())
        {
            _db.Orders.AddRange(
                new Order { OrderNumber = "ORD-2026001", Status = "已出貨" },
                new Order { OrderNumber = "ORD-2026002", Status = "處理中" },
                new Order { OrderNumber = "ORD-2026003", Status = "待處理" }
            );
            _db.SaveChanges();
            return Content("測試資料已成功寫入資料庫！");
        }
        return Content("資料庫內已有資料。");
    }

    // 1. 顯示訂單詳細頁面 (跳轉用)
    // 增加 autoEdit 參數，預設為 false
    public IActionResult Details(string id, bool autoEdit = false)
    {
        var order = _db.Orders.Include(o => o.OrderItems)
                       .FirstOrDefault(o => o.OrderNumber == id);

        if (order == null) return NotFound();

        string currentUser = Request.Cookies["TestUser"]?.Trim() ?? "";
        string dbLocker = WebUtility.HtmlDecode(order.LockedBy ?? "").Trim();

        bool isLockedByMe = !string.IsNullOrEmpty(dbLocker) && dbLocker == currentUser;

        // ✨ 邏輯：只有剛搶完單(autoEdit=true)且鎖定權在我身上，進場才是彩色
        // 平常手動重整，autoEdit 為 false，就會變回唯讀(灰色)
        ViewBag.AutoStartEdit = autoEdit && isLockedByMe;

        bool isLockedByOthers = !string.IsNullOrEmpty(dbLocker) &&
                                dbLocker != currentUser &&
                                order.LockedUntil > DateTime.Now;

        ViewBag.CurrentUser = currentUser;
        ViewBag.IsLockedByOthers = isLockedByOthers;

        return View(order);
    }

    // 取得單筆訂單詳細資訊
    [HttpGet]
    public JsonResult GetOrderDetail(string orderNumber)
    {
        var order = _db.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);

        if (order == null) return Json(null);

        return Json(new
        {
            orderNumber = order.OrderNumber,
            createTime = order.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            status = order.Status
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrder(Order updatedOrder, bool isSubmit = false)
    {
        try
        {
            var existingOrder = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderNumber == updatedOrder.OrderNumber);

            if (existingOrder == null) return Json(new { success = false, message = "找不到該訂單" });

            // --- ✨ 權限檢查邏輯修正 ---
            string currentUser = Request.Cookies["TestUser"];

            // 判斷 1：我是不是目前的鎖定者？
            bool isCurrentLocker = existingOrder.LockedBy == currentUser;

            // 判斷 2：我是不是「正在被搶單」的人？ (對應 SignalR 的 TakeoverRequestedBy)
            // 只要這筆單有人正在請求接管，我們允許原本的人做最後一次「遺言存檔」
            bool isBeingTakenOver = !string.IsNullOrEmpty(existingOrder.TakeoverRequestedBy);

            if (!isCurrentLocker && !isBeingTakenOver)
            {
                return Json(new { success = false, message = "您已失去編輯權，無法儲存變更。" });
            }
            // -------------------------

            if (existingOrder.Status == "已送單" || existingOrder.Status == "已完成")
            {
                return Json(new { success = false, message = "此訂單已進入鎖定狀態，無法修改。" });
            }

            // 1. 更新基本資訊
            existingOrder.CustomerName = updatedOrder.CustomerName;
            existingOrder.PhoneNumber = updatedOrder.PhoneNumber;
            existingOrder.ShippingAddress = updatedOrder.ShippingAddress;
            existingOrder.Note = updatedOrder.Note;

            // 2. 處理商品明細 (你的邏輯沒問題，維持原樣)
            if (updatedOrder.OrderItems != null)
            {
                foreach (var item in updatedOrder.OrderItems)
                {
                    if (item.Id == 0) // 新增
                    {
                        existingOrder.OrderItems.Add(new OrderItem
                        {
                            ProductName = item.ProductName ?? "未命名商品",
                            Color = item.Color,
                            Year = item.Year,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            ItemNote = item.ItemNote
                        });
                    }
                    else // 更新
                    {
                        var existingItem = existingOrder.OrderItems.FirstOrDefault(i => i.Id == item.Id);
                        if (existingItem != null)
                        {
                            existingItem.ProductName = item.ProductName;
                            existingItem.Color = item.Color;
                            existingItem.Year = item.Year;
                            existingItem.Quantity = item.Quantity;
                            existingItem.ItemNote = item.ItemNote;
                        }
                    }
                }
            }

            // 3. 重新計算總額
            existingOrder.TotalAmount = existingOrder.OrderItems.Sum(i => i.UnitPrice * i.Quantity);

            if (isSubmit)
            {
                existingOrder.Status = "已送單";
                // 送出後順便解鎖
                existingOrder.LockedBy = null;
                existingOrder.TakeoverRequestedBy = null;
            }

            await _db.SaveChangesAsync();

            string msg = isSubmit ? "訂單已正式送出並鎖定！" : "暫存成功！";
            return Json(new { success = true, message = msg });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "操作失敗：" + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        try
        {
            // 1. 生成自動編號 (格式: ORD-yyyyMMdd-流水號)
            // 這裡簡單示範：取得當天日期 + 當天第幾筆
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int todayCount = await _db.Orders.CountAsync(o => o.OrderNumber.StartsWith($"ORD-{datePart}"));
            string newOrderNumber = $"ORD-{datePart}-{(todayCount + 1):D3}";

            // 2. 初始化全新訂單物件
            var newOrder = new Order
            {
                OrderNumber = newOrderNumber,
                CreateTime = DateTime.Now,
                Status = "編輯中",
                CustomerName = "", // 預設留空讓使用者填寫
                PhoneNumber = "",
                ShippingAddress = "",
                TotalAmount = 0,
                OrderItems = new List<OrderItem>() // 初始化空清單
            };

            // 3. 存入資料庫
            _db.Orders.Add(newOrder);
            await _db.SaveChangesAsync();

            // 4. 重點：導向到我們設計好的 Details 頁面進行編輯
            // 傳送剛生成的 OrderNumber 作為 ID
            return Json(new { success = true, orderNumber = newOrderNumber });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "建立訂單失敗：" + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UnlockOrder(string id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == id);
        if (order != null)
        {
            order.LockedBy = null;
            order.LockedUntil = null;
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RequestTakeover(string id, string requesterName)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == id);
        if (order != null)
        {
            order.TakeoverRequestedBy = requesterName; // 標記：B 想要這張單
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RequestForceTakeover(string id, string newOwner)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == id);
        if (order == null) return Json(new { success = false, message = "找不到訂單" });

        // 在資料庫標記「有人想搶單」，這能防止 A 在背景存檔時死鎖
        order.TakeoverRequestedBy = newOwner;
        await _db.SaveChangesAsync();

        // ✨ 透過 SignalR 通知目前在頁面上的所有人（主要是通知 A 存檔並退出）
        await _hubContext.Clients.Group(id).SendAsync("OnTakeoverRequested", newOwner);

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> FinalizeTakeover(string id, string newOwner)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == id);
        if (order != null)
        {
            // 正式把鎖定權轉交給新的人
            order.LockedBy = newOwner;
            order.LockedUntil = DateTime.Now.AddMinutes(5);
            order.TakeoverRequestedBy = null; // ✨ 務必清空標記，代表交接完成

            await _db.SaveChangesAsync();

            // (選配) 通知搶單者 B 可以重新整理了
            await _hubContext.Clients.Group(id).SendAsync("OnTakeoverCompleted");
        }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> TryStartEdit(string id, string user)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == id);
        if (order == null) return Json(new { success = false, message = "找不到訂單" });

        string currentUser = user?.Trim();
        // 關鍵：解碼資料庫中的鎖定者名稱
        string dbLocker = WebUtility.HtmlDecode(order.LockedBy ?? "").Trim();

        // 判斷是否被「有效的別人」鎖定
        bool isLockedByOthers = !string.IsNullOrEmpty(dbLocker) &&
                                dbLocker != currentUser &&
                                order.LockedUntil > DateTime.Now;

        if (isLockedByOthers)
        {
            // 有人在改，回傳鎖定者姓名給前端跳 Confirm
            return Json(new
            {
                success = false,
                isLocked = true,
                lockedBy = dbLocker
            });
        }

        // 無人鎖定或是「我自己」重進頁面，直接更新鎖定時間 (續約)
        order.LockedBy = currentUser;
        order.LockedUntil = DateTime.Now.AddMinutes(5); // 給予 5 分鐘編輯權
        order.TakeoverRequestedBy = null; // 確保清空搶單標記

        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }


}
