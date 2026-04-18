using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testweb.Models;

public class OrderItem
{
    [Key]
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    
    // ✨ 新增擴展欄位
    public string? Color { get; set; }      // 顏色
    public string? Year { get; set; }       // 年代/年份
    public string? ItemNote { get; set; }   // 針對單一商品的備註
    public string? Specification { get; set; } // 規格描述 (e.g. 512GB, 13吋)

    public decimal SubTotal => UnitPrice * Quantity;
    
    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
}