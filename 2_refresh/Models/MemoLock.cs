using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models
{
    public class MemoLock
    {
        [Key] // 指定這個當作主鍵
        public int MemoId { get; set; }
        public bool IsEditing { get; set; }
        public string? EditorUsername { get; set; } // 允許為空，代表沒人編輯
    }
}