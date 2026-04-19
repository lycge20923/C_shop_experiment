using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;

namespace MyWebApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _db;

        // 透過依賴注入取得資料庫實例
        public LoginController(AppDbContext db)
        {
            _db = db;
        }

        // 顯示登入畫面 (GET 請求)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password) // 改成非同步 async
        {
            var user = _db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // 1. 建立「證件明細」：告訴系統這個人是誰
                var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Username) };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // 2. 執行登入：這會寫入加密後的 Cookie 到使用者的瀏覽器
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "帳號或密碼錯誤";
                return View();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // 清除瀏覽器的 Cookie 通行證
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 登出後導向首頁
            return RedirectToAction("Index", "Home");
        }
    }
}