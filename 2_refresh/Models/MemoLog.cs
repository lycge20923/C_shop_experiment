using System;
using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models
{
    public class MemoLog
    {
        [Key]
        public int Id { get; set; }
        public int MemoId { get; set; }
        public string Username { get; set; }
        
        // 紀錄動作：例如 "Open", "Reload", "Closed", "EditStart", "EditSave"
        public string Action { get; set; } 
        public DateTime Timestamp { get; set; }
    }
}