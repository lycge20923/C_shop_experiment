using System;
using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models
{
    public class MemoLog
{
    public int Id { get; set; }
    public int MemoId { get; set; }
    public string Username { get; set; }
    public string Action { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
}