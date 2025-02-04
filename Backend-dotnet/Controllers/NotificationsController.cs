using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;

namespace NotepadPlusApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ApplicationDbContext context,
        ILogger<NotificationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications()
    {
        try
        {
            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user ID" });
            }

            _logger.LogInformation($"Getting notifications for user {userId}");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            // Log each notification for debugging
            foreach (var notification in notifications)
            {
                _logger.LogInformation($"Notification: ID={notification.Id}, Type={notification.Type}, IsRead={notification.IsRead}, CreatedAt={notification.CreatedAt}");
            }

            _logger.LogInformation($"Retrieved {notifications.Count} notifications for user {userId}");
            return Ok(new { data = notifications });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, new { message = "Error retrieving notifications" });
        }
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            _logger.LogInformation($"Attempting to mark notification {id} as read");
            
            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user ID" });
            }

            _logger.LogInformation($"Looking for notification with ID {id} for user {userId}");
            
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                _logger.LogWarning($"Notification {id} not found for user {userId}");
                return NotFound(new { message = "Notification not found" });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully marked notification {id} as read for user {userId}");
            return Ok(new { data = notification });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking notification {id} as read");
            return StatusCode(500, new { message = "Error marking notification as read" });
        }
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Marked all notifications as read for user {userId}");
            return Ok(new { message = "All notifications marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { message = "Error marking all notifications as read" });
        }
    }
} 