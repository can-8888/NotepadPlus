using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;
using NotepadPlusApi.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using NotepadPlusApi.Hubs;
using Microsoft.Extensions.Logging;

namespace NotepadPlusApi.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task CreatePublicNoteNotification(Note note)
    {
        _logger.LogInformation($"Creating public note notification for note: {note.Id}");
        
        var users = await _context.Users
            .Where(u => u.Id != note.OwnerId)
            .ToListAsync();

        _logger.LogInformation($"Found {users.Count} users to notify");

        var notifications = users.Select(user => new Notification
        {
            UserId = user.Id,
            NoteId = note.Id,
            Type = (Models.NotificationType)NotificationType.NewPublicNote,
            Message = $"{note.Owner.Username} shared a new public note: {note.Title}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });

        await _context.Notifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();

        foreach (var notification in notifications)
        {
            _logger.LogInformation($"Sending notification to user {notification.UserId}");
            await _hubContext.Clients
                .Group($"User_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification);
        }
    }

    public async Task CreateNoteSharedNotification(Note note, int sharedWithUserId)
    {
        var notification = new Notification
        {
            UserId = sharedWithUserId,
            NoteId = note.Id,
            Type = (Models.NotificationType)NotificationType.NoteShared,
            Message = $"{note.Owner.Username} shared a note with you: {note.Title}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        await _hubContext.Clients
            .Group($"User_{notification.UserId}")
            .SendAsync("ReceiveNotification", notification);
    }
} 