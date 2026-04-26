using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using System.Linq;
using System;

namespace MyWebApp.Controllers
{
    [Route("Log/[action]/{id?}")]
    public class LogController : Controller
    {
        private readonly AppDbContext _db;

        public LogController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var logs = _db.MemoLogs.OrderByDescending(l => l.StartTime).ToList();
            return View(logs);
        }

        // ✅ 新增：開始紀錄日誌 (當進入編輯模式時呼叫)
        [HttpPost]
        public IActionResult StartLog(int id)
        {
            var log = new MemoLog
            {
                MemoId = id,
                Username = User.Identity?.Name ?? "Anonymous",
                Action = "editing",
                StartTime = DateTime.Now,
                EndTime = null // 代表還在進行中
            };

            _db.MemoLogs.Add(log);
            _db.SaveChanges();

            // 回傳剛剛產生的資料 ID，讓前端存起來
            return Json(new { logId = log.Id });
        }

        // ✅ 新增：結束紀錄日誌 (當存檔、取消或關閉視窗時呼叫)
        [HttpPost]
        public IActionResult FinishLog(int logId, string actionType)
        {
            var log = _db.MemoLogs.Find(logId);
            if (log != null)
            {
                log.Action = actionType; // 會是 "saved" 或 "closed"
                log.EndTime = DateTime.Now;
                _db.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "找不到該筆日誌紀錄" });
        }
    }
}