namespace MyWebApp.Models
{
    public class Memo
    {
        // 系統會自動將 Id 當作主鍵，並且每次新增都會自動 +1，正好作為「訂單號碼」
        public int Id { get; set; } 
        
        // Memo 的標題或內容
        public string Title { get; set; } 
    }
}