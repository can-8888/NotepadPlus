using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;

namespace NotepadPlusApi.Controllers
{
    [ApiController]
    [Route("api/notification-preferences")]
    public class NotificationPreferencesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationPreferencesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<UserNotificationPreferences>> GetPreferences()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var prefs = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (prefs == null)
            {
                prefs = new UserNotificationPreferences { UserId = userId };
                _context.UserNotificationPreferences.Add(prefs);
                await _context.SaveChangesAsync();
            }

            return prefs;
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePreferences(UserNotificationPreferences preferences)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var existing = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existing == null)
            {
                return NotFound();
            }

            existing.NotifyOnPublicNotes = preferences.NotifyOnPublicNotes;
            existing.NotifyOnSharedNotes = preferences.NotifyOnSharedNotes;
            existing.EmailNotifications = preferences.EmailNotifications;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
} 