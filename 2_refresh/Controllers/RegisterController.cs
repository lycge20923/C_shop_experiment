using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models; // 記得確認這裡是你的專案名稱
using System.Linq;

namespace MyWebApp.Controllers
{
    public class RegisterController : Controller
    {
        private readonly AppDbContext _db;

        public RegisterController(AppDbContext db)
        {
            _db = db;
        }

        // 顯示註冊畫面 (GET)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 接收使用者送出的註冊資料 (POST)
        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            // 1. 檢查帳號是否已經存在
            if (_db.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "這個帳號已經有人使用了，請換一個！";
                return View();
            }

            // 2. 建立新帳號並存入資料庫
            var newUser = new User 
            { 
                Username = username, 
                Password = password 
            };
            
            _db.Users.Add(newUser);
            _db.SaveChanges(); // 儲存變更

            // 3. 註冊成功，帶著成功訊息導向回登入頁面
            TempData["SuccessMessage"] = "帳號建立成功！請使用新帳號登入。";
            return RedirectToAction("Index", "Login");
        }
    }
}