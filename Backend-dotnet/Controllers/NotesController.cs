using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;
using Microsoft.Extensions.Logging;
using NotepadPlusApi.Services.Interfaces;
using NotepadPlusApi.Services;
using NotepadPlusApi.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace NotepadPlusApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotesController> _logger;
    private readonly INoteService _noteService;
    private readonly INotificationService _notificationService;

    public NotesController(
        ApplicationDbContext context,
        ILogger<NotesController> logger,
        INoteService noteService,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _noteService = noteService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Note>>> GetNotes()
    {
        try
        {
            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user ID" });
            }

            _logger.LogInformation($"Getting notes for user {userId}");
            var notes = await _noteService.GetNotesAsync(userId);
            _logger.LogInformation($"Found {notes.Count()} notes for user {userId}");
            
            return Ok(new { data = notes });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning($"Notes not found: {ex.Message}");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notes");
            return StatusCode(500, new { message = "An error occurred while retrieving notes" });
        }
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string> Health()
    {
        _logger.LogInformation("Health check endpoint hit");
        return Ok("API is running");
    }

    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<string> Ping()
    {
        _logger.LogInformation("Ping endpoint hit");
        return Ok("API is running");
    }

    public class CreateNoteRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public NoteStatus Status { get; set; }
        public bool IsPublic { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<Note>> CreateNote(NoteDto noteDto)
    {
        try
        {
            _logger.LogInformation("Creating new note");

            var userIdString = Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return BadRequest(new { message = "User ID not provided" });
            }

            var note = new Note
            {
                Title = noteDto.Title,
                Content = noteDto.Content,
                Category = noteDto.Category,
                OwnerId = userId,
                Status = NoteStatus.Personal,
                IsPublic = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdNote = await _noteService.CreateNoteAsync(note);
            return CreatedAtAction(
                nameof(GetNotes), 
                new { id = createdNote.Id }, 
                new { data = FormatNoteResponse(createdNote) }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note");
            return StatusCode(500, new { message = "Internal server error while creating note" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(int id, NoteDto noteDto)
    {
        try
        {
            var userIdString = Request.Headers["UserId"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return BadRequest(new { message = "User ID not provided" });
            }

            // Convert NoteDto to Note
            var note = new Note
            {
                Id = id,
                Title = noteDto.Title,
                Content = noteDto.Content,
                Category = noteDto.Category,
                UpdatedAt = DateTime.UtcNow
            };

            var existingNote = await _noteService.UpdateNoteAsync(id, note);
            if (existingNote.OwnerId != userId)
            {
                return Forbid();
            }

            return Ok(new { data = FormatNoteResponse(existingNote) });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Note not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        try
        {
            _logger.LogInformation($"Attempting to delete note {id}");

            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user ID" });
            }

            // Include related entities to properly handle cascading deletes
            var note = await _context.Notes
                .Include(n => n.NoteShares)
                .Include(n => n.Notifications)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                _logger.LogWarning($"Note {id} not found");
                return NotFound(new { message = "Note not found" });
            }

            if (note.OwnerId != userId)
            {
                _logger.LogWarning($"User {userId} attempted to delete note {id} but is not the owner");
                return Unauthorized(new { message = "Only the note owner can delete it" });
            }

            // Remove related entities first
            if (note.NoteShares != null)
            {
                _context.NoteShares.RemoveRange(note.NoteShares);
            }
            if (note.Notifications != null)
            {
                _context.Notifications.RemoveRange(note.Notifications);
            }

            // Then remove the note
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully deleted note {id}");
            return Ok(new { message = "Note deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting note {id}");
            return StatusCode(500, new { message = "Internal server error while deleting note" });
        }
    }

    [HttpPost("{id}/share")]
    public async Task<ActionResult<Note>> ShareNote(int id, [FromBody] ShareNoteRequest request)
    {
        try
        {
            var note = await _context.Notes
                .Include(n => n.Owner)  // Include Owner for notification message
                .FirstOrDefaultAsync(n => n.Id == id);
            
            if (note == null)
                return NotFound(new { message = "Note not found" });

            // Share the note
            await _noteService.ShareNoteAsync(id, request.CollaboratorId);
            
            // Create notification for the user it was shared with
            await _notificationService.CreateNoteSharedNotification(note, request.CollaboratorId);

            return Ok(new { 
                message = "Note shared successfully",
                noteId = id,
                sharedWithUserId = request.CollaboratorId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing note");
            return StatusCode(500, new { message = "Error sharing note" });
        }
    }

    [HttpGet("shared")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Note>>> GetSharedNotes()
    {
        try
        {
            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user ID" });
            }

            _logger.LogInformation($"Getting shared notes for user {userId}");
            var notes = await _noteService.GetSharedNotesAsync(userId);
            return Ok(new { data = notes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shared notes");
            return StatusCode(500, new { message = "Error retrieving shared notes" });
        }
    }

    [HttpPut("{id}/make-public")]
    public async Task<ActionResult<Note>> MakeNotePublic(int id)
    {
        try
        {
            _logger.LogInformation($"Making note {id} public");
            
            var note = await _context.Notes
                .Include(n => n.Owner)  // Include Owner for notification message
                .FirstOrDefaultAsync(n => n.Id == id);
                
            if (note == null)
            {
                return NotFound(new { message = $"Note with ID {id} not found" });
            }

            note.IsPublic = true;
            note.Status = NoteStatus.Public;
            note.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            // Create notification for all users
            await _notificationService.CreatePublicNoteNotification(note);
            
            return Ok(new { data = note });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error making note {id} public");
            return StatusCode(500, new { message = "Error making note public" });
        }
    }

    [HttpGet("public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Note>>> GetPublicNotes()
    {
        try
        {
            // Fix header warnings by using Append instead of Add
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                _logger.LogWarning("Invalid user ID in token");
                return Unauthorized(new { message = "Invalid user ID" });
            }

            _logger.LogInformation($"Getting public notes for user {userId}");
            var notes = await _noteService.GetPublicNotesAsync(userId);
            return Ok(new { data = notes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public notes");
            return StatusCode(500, new { message = "Error retrieving public notes" });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<Note>> UpdateNoteStatus(int id, [FromBody] NoteStatus status)
    {
        try
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
                return NotFound(new { message = "Note not found" });

            note.Status = status;
            note.IsPublic = status == NoteStatus.Public;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { data = note });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("debug/stats")]
    public async Task<ActionResult> GetDebugStats()
    {
        try
        {
            var stats = new
            {
                TotalNotes = await _context.Notes.CountAsync(),
                PublicNotes = await _context.Notes.CountAsync(n => n.IsPublic),
                SharedNotes = await _context.Notes
                    .Include(n => n.NoteShares)
                    .CountAsync(n => n.NoteShares.Any()),
                NoteShares = await _context.NoteShares.CountAsync(),
                Users = await _context.Users.CountAsync()
            };

            _logger.LogInformation($"Debug stats: {System.Text.Json.JsonSerializer.Serialize(stats)}");
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug stats");
            return StatusCode(500, new { message = "Error retrieving debug stats" });
        }
    }

    [HttpPost("debug/create-test-data")]
    public async Task<ActionResult> CreateTestData()
    {
        try
        {
            if (!int.TryParse(HttpContext.Request.Headers["UserId"], out int userId))
                return Unauthorized();

            // Create a public note
            var publicNote = new Note
            {
                Title = "Test Public Note",
                Content = "This is a test public note",
                Category = "Test",
                OwnerId = userId,
                IsPublic = true,
                Status = NoteStatus.Public,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Notes.Add(publicNote);

            // Create a shared note
            var sharedNote = new Note
            {
                Title = "Test Shared Note",
                Content = "This is a test shared note",
                Category = "Test",
                OwnerId = userId,
                IsPublic = false,
                Status = NoteStatus.Shared,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Notes.Add(sharedNote);

            await _context.SaveChangesAsync();

            // Share the note with another user (if exists)
            var otherUser = await _context.Users.FirstOrDefaultAsync(u => u.Id != userId);
            if (otherUser != null)
            {
                await _noteService.ShareNoteAsync(sharedNote.Id, otherUser.Id);
            }

            return Ok(new { message = "Test data created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test data");
            return StatusCode(500, new { message = "Error creating test data" });
        }
    }

    private async Task EnsureTestData(int userId)
    {
        if (!await _context.Notes.AnyAsync(n => n.OwnerId == userId))
        {
            var testNote = new Note
            {
                Title = "Test Note",
                Content = "This is a test note",
                Category = "Test",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OwnerId = userId,
                Status = NoteStatus.Personal,
                IsPublic = false
            };

            _context.Notes.Add(testNote);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Created test note for user {userId}");
        }
    }

    private object FormatNoteResponse(Note note)
    {
        return new
        {
            id = note.Id,
            title = note.Title,
            content = note.Content,
            category = note.Category,
            ownerId = note.OwnerId,
            status = note.Status,
            isPublic = note.IsPublic,
            createdAt = note.CreatedAt,
            updatedAt = note.UpdatedAt,
            owner = note.Owner?.Username,
            statusText = note.Status switch
            {
                NoteStatus.Public => "Public",
                NoteStatus.Shared => "Shared",
                NoteStatus.Personal => "Personal",
                _ => "Personal"
            }
        };
    }

    [HttpGet("debug/notes")]
    public async Task<ActionResult> GetAllNotesDebug()
    {
        try
        {
            var notes = await _context.Notes
                .Include(n => n.Owner)
                .Include(n => n.NoteShares)
                    .ThenInclude(s => s.User)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.OwnerId,
                    n.IsPublic,
                    n.Status,
                    SharedWithCount = n.NoteShares.Count,
                    SharedWithUsers = n.NoteShares.Select(s => new { s.UserId, s.User.Username }),
                    SharedWith = n.NoteShares.Select(s => new { s.UserId, s.User.Username })
                })
                .ToListAsync();

            return Ok(new
            {
                notes = notes,
                count = notes.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug notes info");
            return StatusCode(500, new { message = "Error getting debug notes info" });
        }
    }

    [HttpPost("{noteId}/share-with/{userId}")]
    public async Task<ActionResult> ShareNoteWithUser(int noteId, int userId)
    {
        try
        {
            await _noteService.ShareNoteAsync(noteId, userId);
            return Ok(new { message = "Note shared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing note");
            return StatusCode(500, new { message = "Error sharing note" });
        }
    }

    [HttpGet("debug/note-status/{id}")]
    public async Task<ActionResult> GetNoteStatusDebug(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note == null)
            return NotFound();

        return Ok(new
        {
            NoteId = id,
            Status = note.Status,                // Enum value (0,1,2)
            StatusName = note.Status.ToString(), // String value (Personal,Shared,Public)
            IsPublic = note.IsPublic,
            UpdatedAt = note.UpdatedAt
        });
    }

    [HttpGet("debug/check-owner/{noteId}")]
    public async Task<ActionResult> CheckNoteOwner(int noteId)
    {
        var note = await _context.Notes
            .Include(n => n.Owner)
            .Include(n => n.NoteShares)
            .FirstOrDefaultAsync(n => n.Id == noteId);

        if (note == null)
            return NotFound();

        var owner = await _context.Users.FindAsync(note.OwnerId);

        return Ok(new
        {
            NoteId = note.Id,
            NoteTitle = note.Title,
            OwnerId = note.OwnerId,
            HasOwnerNavigation = note.Owner != null,
            OwnerInDb = owner != null,
            OwnerDetails = owner != null ? new { owner.Id, owner.Username, owner.Email } : null
        });
    }

    [HttpGet("debug/note/{id}")]
    public async Task<ActionResult> GetNoteDebugInfo(int id)
    {
        try
        {
            _logger.LogInformation($"Getting debug info for note {id}");
            
            var note = await _context.Notes
                .Include(n => n.Owner)
                .Include(n => n.NoteShares)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
                return NotFound();

            return Ok(new
            {
                NoteId = id,
                Title = note.Title,
                Status = note.Status,
                IsPublic = note.IsPublic,
                OwnerId = note.OwnerId,
                SharedWith = note.NoteShares.Select(s => new
                {
                    UserId = s.UserId,
                    Username = s.User?.Username
                }).ToList(),
                SharedCount = note.NoteShares.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note debug info");
            return StatusCode(500, new { message = "Error getting note debug info" });
        }
    }

    [HttpGet("debug/test-db")]
    public async Task<ActionResult> TestDatabase()
    {
        try
        {
            var dbWorks = await _context.Database.CanConnectAsync();
            var noteCount = await _context.Notes.CountAsync();
            var userCount = await _context.Users.CountAsync();
            
            return Ok(new
            {
                DatabaseConnected = dbWorks,
                NoteCount = noteCount,
                UserCount = userCount,
                Provider = _context.Database.ProviderName,
                TimeUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("debug/sharing-status/{id}")]
    public async Task<ActionResult> GetNoteSharingDebug(int id)
    {
        var note = await _context.Notes
            .Include(n => n.Owner)
            .Include(n => n.NoteShares)
                .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null)
            return NotFound();

        return Ok(new
        {
            NoteId = id,
            Title = note.Title,
            Status = note.Status,
            IsPublic = note.IsPublic,
            OwnerId = note.OwnerId,
            SharedWith = note.NoteShares.Select(s => new 
            { 
                UserId = s.UserId,
                Username = s.User?.Username
            }).ToList(),
            SharedCount = note.NoteShares.Count
        });
    }

    [HttpGet("debug/shares")]
    public async Task<ActionResult> GetAllShares()
    {
        try
        {
            var shares = await _context.NoteShares
                .Select(s => new { s.NoteId, s.UserId, s.SharedAt })
                .ToListAsync();

            var notes = await _context.Notes
                .Where(n => shares.Select(s => s.NoteId).Contains(n.Id))
                .Select(n => new { n.Id, n.Title, n.OwnerId })
                .ToListAsync();

            return Ok(new { 
                shares = shares,
                notes = notes,
                totalShares = shares.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shares debug info");
            return StatusCode(500, new { message = "Error getting shares debug info" });
        }
    }

    // Regular share note request
    public class ShareNoteRequest
    {
        public int CollaboratorId { get; set; }
    }

    // Debug share note request
    public class DebugShareNoteRequest
    {
        public int NoteId { get; set; }
        public int UserId { get; set; }
    }

    // Update the debug endpoint to use the new class
    [HttpPost("debug/share-note")]
    public async Task<ActionResult> TestShareNote([FromBody] DebugShareNoteRequest request)
    {
        try
        {
            var note = await _context.Notes.FindAsync(request.NoteId);
            if (note == null)
                return NotFound(new { message = "Note not found" });

            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var share = new NoteShare
            {
                NoteId = request.NoteId,
                UserId = request.UserId,
                SharedAt = DateTime.UtcNow
            };

            _context.NoteShares.Add(share);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Note shared successfully",
                share = new
                {
                    share.NoteId,
                    share.UserId,
                    share.SharedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test share");
            return StatusCode(500, new { message = "Error sharing note" });
        }
    }

    [HttpPost("debug/create-test-share")]
    public async Task<ActionResult> CreateTestShare()
    {
        try
        {
            if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }

            // Find a note that isn't owned by the current user
            var noteToShare = await _context.Notes
                .FirstOrDefaultAsync(n => n.OwnerId != userId);

            if (noteToShare == null)
            {
                return NotFound(new { message = "No notes available to share" });
            }

            // Create share record
            var share = new NoteShare
            {
                NoteId = noteToShare.Id,
                UserId = userId,
                SharedAt = DateTime.UtcNow
            };

            _context.NoteShares.Add(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created test share: Note {noteToShare.Id} shared with user {userId}");
            return Ok(new { 
                message = "Test share created", 
                noteId = noteToShare.Id, 
                userId = userId 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test share");
            return StatusCode(500, new { message = "Error creating test share" });
        }
    }
}