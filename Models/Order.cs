using System.ComponentModel.DataAnnotations;

namespace testweb.Models;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    public string Status { get; set; } = "待處理";

    public string CustomerName { get; set; } = "未填寫";
    public string PhoneNumber { get; set; } = "未填寫";
    public string ShippingAddress { get; set; } = "未填寫";
    public decimal TotalAmount { get; set; } = 0;
    public string Note { get; set; } = "無備註";

    // ✨ 關聯屬性：一筆訂單會有多個商品明細
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public string? LockedBy { get; set; }        // 存放正在編輯的使用者名稱或 ID
    public DateTime? LockedUntil { get; set; }   // 鎖定失效時間（防止使用者沒關網頁就下班）

    public string? TakeoverRequestedBy { get; set; } // 紀錄誰發起了搶單請求
}