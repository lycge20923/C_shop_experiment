namespace MyWebApp.Models
{
    public class User
    {
        public int Id { get; set; } // 主鍵
        public string Username { get; set; }
        public string Password { get; set; } // 注意：實際專案應儲存 Hash 後的密碼
    }
}