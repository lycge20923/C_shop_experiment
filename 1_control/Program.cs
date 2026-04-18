
using testweb.Models;
using Microsoft.EntityFrameworkCore;
using testweb.Data; // 引用你剛剛建的資料夾
using testweb.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=guestbook.db"));
builder.Services.AddControllersWithViews(); // Add services to the container.
builder.Services.AddSignalR(); // 註冊 SignalR

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapHub<OrderHub>("/orderHub"); // ✨ 現在 app 已經被定義，且 OrderHub 也有引用了

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    // 確保資料庫已建立 (跟 dotnet ef database update 效果類似，但更保險)
    context.Database.EnsureCreated();

    // 如果訂單表是空的，就自動新增
    if (!context.Orders.Any())
    {
        context.Orders.Add(new Order
        {
            OrderNumber = "ORD-2026001",
            Status = "編輯中",
            CustomerName = "張小明",
            PhoneNumber = "0912-345-678",
            ShippingAddress = "台北市中山區南京東路一段 1 號",
            TotalAmount = 36900,
            Note = "請於下午兩點後配送",
            // ✨ 改用這個方式新增商品
            OrderItems = new List<OrderItem>
        {
            new OrderItem { ProductName = "iPhone 15 Pro", UnitPrice = 35000, Quantity = 1, Color = "綠色" },
            new OrderItem { ProductName = "保護殼", UnitPrice = 1900, Quantity = 1, ItemNote = "測試" }
        }
        });
        // ... 其他訂單以此類推
        context.SaveChanges();
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
