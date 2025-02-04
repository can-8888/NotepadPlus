using System;

namespace NotepadPlusApi.Models;

public class UserNotificationPreferences
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool NotifyOnPublicNotes { get; set; } = true;
    public bool NotifyOnSharedNotes { get; set; } = true;
    public bool EmailNotifications { get; set; } = false;
} 