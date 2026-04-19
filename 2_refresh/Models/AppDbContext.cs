using Microsoft.EntityFrameworkCore;

namespace MyWebApp.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        
        // 新增這一行：讓資料庫知道我們要多一張名為 Memos 的資料表
        public DbSet<Memo> Memos { get; set; } 
        public DbSet<MemoLock> MemoLocks { get; set; }
    }
}