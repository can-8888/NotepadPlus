using System;

namespace NotepadPlusApi.Models;

public enum NotificationType
{
    NewPublicNote,
    NoteShared
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public int? NoteId { get; set; }
    public Note? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
} 