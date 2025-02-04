using Microsoft.AspNetCore.SignalR;
using NotepadPlusApi.Models;
using Microsoft.Extensions.Logging;

namespace NotepadPlusApi.Hubs;

public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userIdHeader = Context.GetHttpContext()?.Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdHeader))
            {
                _logger.LogWarning("User ID not found in headers");
                throw new HubException("User ID not found");
            }

            _logger.LogInformation($"Client connected: {Context.ConnectionId}, User: {userIdHeader}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userIdHeader}");
            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync");
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdHeader = Context.GetHttpContext()?.Request.Headers["UserId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userIdHeader))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userIdHeader}");
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}, User: {userIdHeader}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinUserGroup(string userId)
    {
        try
        {
            var userIdHeader = Context.GetHttpContext()?.Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdHeader))
            {
                _logger.LogWarning("User ID not found in headers");
                throw new HubException("Unauthorized group access");
            }

            if (userId != userIdHeader)
            {
                _logger.LogWarning($"User {userIdHeader} attempted to join group for user {userId}");
                throw new HubException("Unauthorized group access");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation($"User {userId} joined their notification group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error joining user group {userId}");
            throw;
        }
    }

    public async Task LeaveUserGroup(string userId)
    {
        try
        {
            var tokenUserId = Context.User?.FindFirst("id")?.Value;
            if (userId != tokenUserId)
            {
                _logger.LogWarning($"User {tokenUserId} attempted to leave group for user {userId}");
                throw new HubException("Unauthorized group access");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            _logger.LogInformation($"User {userId} left their notification group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error leaving user group {userId}");
            throw;
        }
    }

    public async Task SendNotification(string userId, Notification notification)
    {
        try
        {
            var tokenUserId = Context.User?.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(tokenUserId))
            {
                _logger.LogWarning("User ID not found in token when sending notification");
                throw new HubException("Unauthorized notification send");
            }

            // Only allow sending to own group or if user has admin role
            if (userId != tokenUserId && !Context.User.IsInRole("Admin"))
            {
                _logger.LogWarning($"User {tokenUserId} attempted to send notification to user {userId}");
                throw new HubException("Unauthorized notification send");
            }

            _logger.LogInformation($"Sending notification to user {userId}");
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification to user {userId}");
            throw;
        }
    }
} 