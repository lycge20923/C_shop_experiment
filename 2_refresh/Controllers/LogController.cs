using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using System.Linq;

namespace MyWebApp.Controllers
{
    // 可以加上 [Authorize] 限制登入才能看，這裡先省略方便測試
    public class LogController : Controller
    {
        private readonly AppDbContext _db;

        public LogController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // 將 Log 撈出來，並依照時間遞減排序 (最新的在最上面)
            var logs = _db.MemoLogs.OrderByDescending(l => l.Timestamp).ToList();
            return View(logs);
        }
    }
}