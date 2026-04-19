using Microsoft.EntityFrameworkCore; // 解決找不到 UseInMemoryDatabase 的關鍵
using Microsoft.AspNetCore.Authentication.Cookies;
using MyWebApp.Models;               // 記得把 MyWebApp 換成你的專案名稱

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// 註冊 In-Memory 資料庫
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index"; // 如果沒登入被擋下，自動跳轉到這裡
    });

var app = builder.Build();

// 初始化測試資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Users.Any())
    {
        db.Users.Add(new User { Username = "admin", Password = "123" });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();