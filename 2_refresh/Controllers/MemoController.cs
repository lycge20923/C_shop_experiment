using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace MyWebApp.Controllers
{
    [Authorize]
    public class MemoController : Controller
    {
        private readonly AppDbContext _db;

        public MemoController(AppDbContext db)
        {
            _db = db;
        }

        // 1. 顯示 Memo 清單 (GET)
        [HttpGet]
        public IActionResult Index()
        {
            var allMemos = _db.Memos.ToList();
            return View(allMemos);
        }

        // 2. 【新增的】顯示填寫 Memo 的空白表單頁面 (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View(); // 這會去尋找 Views/Memo/Create.cshtml
        }

        // 3. 接收使用者填好的表單並存入資料庫 (POST)
        [HttpPost]
        public IActionResult Create(string title)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var newMemo = new Memo { Title = title };
                _db.Memos.Add(newMemo);
                _db.SaveChanges();
            }

            // 存檔完成後，自動跳轉回清單頁面
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Details(int id)
        {
            var memo = _db.Memos.FirstOrDefault(m => m.Id == id);
            if (memo == null) return RedirectToAction("Index");

            // 【新增】檢查目前資料庫的鎖，是不是屬於「當下這個使用者」的？
            var currentUser = User.Identity.Name;
            var lockRecord = _db.MemoLocks.FirstOrDefault(l => l.MemoId == id);

            bool isLockedByMe = (lockRecord != null && lockRecord.IsEditing && lockRecord.EditorUsername == currentUser);

            // 把這個布林值存進 ViewBag，讓 HTML 可以讀取到
            ViewBag.IsLockedByMe = isLockedByMe;

            return View(memo);
        }
        // 5. 顯示編輯頁面 (GET: /Memo/Edit/5)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var memo = _db.Memos.FirstOrDefault(m => m.Id == id);
            if (memo == null) return RedirectToAction("Index");

            return View(memo); // 把找到的資料傳給編輯頁面
        }

        // 2. 【修改】原本的 Edit 方法 (加入 Save 的紀錄)
        [HttpPost]
        public IActionResult Edit(int id, string title)
        {
            var memo = _db.Memos.FirstOrDefault(m => m.Id == id);
            var currentUser = User.Identity.Name;
            var lockRecord = _db.MemoLocks.FirstOrDefault(l => l.MemoId == id);

            if (lockRecord == null || !lockRecord.IsEditing || lockRecord.EditorUsername != currentUser)
            {
                TempData["ErrorMessage"] = $"儲存失敗！目前鎖定狀態異常。";
                return RedirectToAction("Details", new { id = id });
            }

            if (memo != null && !string.IsNullOrWhiteSpace(title))
            {
                memo.Title = title;
                lockRecord.IsEditing = false;
                lockRecord.EditorUsername = null;

                // 【新增】：在成功修改資料庫的這一刻，寫入 Save 紀錄
                _db.MemoLogs.Add(new MemoLog
                {
                    MemoId = id,
                    Username = currentUser,
                    Action = "saved",
                    Timestamp = DateTime.Now
                });

                _db.SaveChanges();
                TempData["SuccessMessage"] = "儲存成功！";
            }

            return RedirectToAction("Details", new { id = id });
        }
        // 【新增】嘗試上鎖 API (給 JavaScript 呼叫)
        [HttpPost]
        public IActionResult TryLock(int id, bool force = false)
        {
            var currentUser = User.Identity.Name;
            var lockRecord = _db.MemoLocks.FirstOrDefault(l => l.MemoId == id);

            // 情況 A：沒人鎖過
            if (lockRecord == null)
            {
                _db.MemoLocks.Add(new MemoLock { MemoId = id, IsEditing = true, EditorUsername = currentUser });
                _db.SaveChanges();
                return Json(new { success = true });
            }

            // 情況 B：沒人在編輯、目前是自己編輯、或者使用者按下「強制搶奪(force = true)」
            if (!lockRecord.IsEditing || lockRecord.EditorUsername == currentUser || force)
            {
                lockRecord.IsEditing = true;
                lockRecord.EditorUsername = currentUser; // 把編輯者改成自己
                _db.SaveChanges();
                return Json(new { success = true });
            }

            // 情況 C：別人在編輯，且尚未確認搶奪 -> 回傳 requireConfirm 讓前端跳警告
            return Json(new
            {
                success = false,
                requireConfirm = true,
                message = $"警告：目前【{lockRecord.EditorUsername}】正在編輯這筆訂單！\n\n請問是否要強制搶奪編輯權？(這會導致對方的心血白費)"
            });
        }
        // ----------------------------------------------------
        // 【新增】解除鎖定 API (當點擊取消時呼叫)
        [HttpPost]
        public IActionResult Unlock(int id)
        {
            var currentUser = User.Identity.Name;
            var lockRecord = _db.MemoLocks.FirstOrDefault(l => l.MemoId == id);

            // 只有鎖的主人可以解鎖
            if (lockRecord != null && lockRecord.EditorUsername == currentUser)
            {
                lockRecord.IsEditing = false;
                lockRecord.EditorUsername = null;
                _db.SaveChanges();
            }
            return Json(new { success = true });
        }

        // ==============================================================================
        // 給 LLM 的架構提示：
        // 1. 這裡實作了「後端日誌合併 (Log Consolidation)」機制。
        // 2. 因為瀏覽器的 pagehide 無法區分「關閉」或「重整」，前端一律先發送 "closed"。
        // 3. 若前端隨後發送 "refreshed"，後端會尋找 10 秒內的 "closed" 日誌並將其覆蓋。
        // 4. 注意：API 參數名稱必須使用 `actionType`，絕對不能使用 `action`，以避免與 ASP.NET Core MVC 的系統保留字發生衝突。
        // ==============================================================================
        [HttpPost]
        public IActionResult RecordLog(int id, string actionType)
        {
            var currentUser = User.Identity.Name ?? "System";

            // ==========================================
            // 【核心魔法：日誌合併】如果是重整，去改寫剛剛那筆 closed
            // ==========================================
            if (actionType == "refreshed")
            {
                // 定義一個「寬限期」，例如過去 10 秒內的日誌
                var recentTimeLimit = DateTime.Now.AddSeconds(-10);

                // 找出這個訂單、這個使用者，最新的一筆日誌
                var lastLog = _db.MemoLogs
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefault(l => l.MemoId == id && l.Username == currentUser);

                // 如果最新的一筆剛好是 "closed"，且發生在幾秒鐘前
                if (lastLog != null && lastLog.Action == "closed" && lastLog.Timestamp >= recentTimeLimit)
                {
                    // 直接把原本的 closed 改成 refreshed！(不會產生新紀錄)
                    lastLog.Action = "refreshed";
                    lastLog.Timestamp = DateTime.Now; // 更新時間點
                    _db.SaveChanges();

                    return Json(new { success = true });
                }
            }

            // 如果不是 refreshed，或者是真的關閉太久才重開，就正常新增一筆
            _db.MemoLogs.Add(new MemoLog
            {
                MemoId = id,
                Username = currentUser,
                Action = actionType,
                Timestamp = DateTime.Now
            });

            _db.SaveChanges();
            return Json(new { success = true });
        }

    }
}