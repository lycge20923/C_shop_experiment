using Microsoft.EntityFrameworkCore;
using testweb.Models;

namespace testweb.Data
{
    // 這個類別繼承自 DbContext，是 EF Core 的核心
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; } // ✨ 新增這一行
    }
}