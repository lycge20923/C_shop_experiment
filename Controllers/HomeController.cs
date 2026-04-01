using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using testweb.Models;

namespace testweb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // --- ✨ 新增在這裡 ✨ ---
    [HttpPost] 
    public JsonResult GetHelloData(string name)
    {
        var result = new {
            message = $"你好 {name}！這是從 ASP.NET 後端傳回來的訊息。",
            serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        return Json(result);
    }
    // -----------------------
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
