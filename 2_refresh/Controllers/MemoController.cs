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

        // 6. 接收修改後的資料 (POST: /Memo/Edit/5)
        [HttpPost]
        public IActionResult Edit(int id, string title)
        {
            var memo = _db.Memos.FirstOrDefault(m => m.Id == id);
            var currentUser = User.Identity.Name;
            var lockRecord = _db.MemoLocks.FirstOrDefault(l => l.MemoId == id);

            // 【新增防線】：如果鎖的紀錄不見了、沒在上鎖狀態、或是編輯者「不是自己」(被搶走了)
            if (lockRecord == null || !lockRecord.IsEditing || lockRecord.EditorUsername != currentUser)
            {
                // 儲存失敗！把錯誤訊息塞進 TempData，稍後在畫面上顯示
                var currentOwner = lockRecord?.EditorUsername ?? "其他人";
                TempData["ErrorMessage"] = $"儲存失敗！【{currentOwner}】目前為最終編輯者，您先前的編輯內容已失效。";

                // 把他踢回唯讀畫面
                return RedirectToAction("Details", new { id = id });
            }

            // 如果鎖還在自己身上，正常存檔並解鎖
            if (memo != null && !string.IsNullOrWhiteSpace(title))
            {
                memo.Title = title;
                lockRecord.IsEditing = false;
                lockRecord.EditorUsername = null;
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
    }
}