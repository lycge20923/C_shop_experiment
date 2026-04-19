using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models; // 記得確認這裡是你的專案名稱
using System.Linq;

namespace MyWebApp.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }

        // 顯示所有使用者的畫面
        public IActionResult Index()
        {
            // 【這就是查詢語法！】
            // _db.Users 相當於 SQL 的 SELECT * FROM Users
            // .ToList() 會把查詢結果轉換成一個 C# 的列表 (List)
            var allUsers = _db.Users.ToList();

            // 將查詢到的資料傳遞給網頁畫面 (View)
            return View(allUsers);
        }
    }
}