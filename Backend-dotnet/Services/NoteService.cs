using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NotepadPlusApi.Models;
using NotepadPlusApi.Data;
using Microsoft.Extensions.Logging;
using System;
using NotepadPlusApi.Services.Interfaces;
using System.Text.Json;
using NotepadPlusApi.Exceptions;

namespace NotepadPlusApi.Services
{
    public class NoteService : INoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoteService> _logger;

        public NoteService(ApplicationDbContext context, ILogger<NoteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Note>> GetNotesAsync(int userId)
        {
            return await _context.Notes
                .Include(n => n.Owner)
                .Where(n => n.OwnerId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Note?> GetNoteByIdAsync(int id)
        {
            return await _context.Notes
                .Include(n => n.Owner)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Note> CreateNoteAsync(Note note)
        {
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<Note> UpdateNoteAsync(int id, Note note)
        {
            var existingNote = await _context.Notes.FindAsync(id);
            if (existingNote == null)
                throw new Exception("Note not found");

            // Only update non-null properties
            if (!string.IsNullOrEmpty(note.Title))
                existingNote.Title = note.Title;
            if (!string.IsNullOrEmpty(note.Content))
                existingNote.Content = note.Content;
            if (!string.IsNullOrEmpty(note.Category))
                existingNote.Category = note.Category;
            
            // Update status and visibility if provided
            if (note.Status != default(NoteStatus))
                existingNote.Status = note.Status;
            if (note.IsPublic)
                existingNote.IsPublic = note.IsPublic;

            existingNote.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation($"Updating note {id} - Status: {existingNote.Status}, IsPublic: {existingNote.IsPublic}");
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Note {id} updated successfully - New Status: {existingNote.Status}, IsPublic: {existingNote.IsPublic}");
            return existingNote;
        }

        public async Task DeleteNoteAsync(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Note>> GetPublicNotesAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Getting public notes for user {userId}");
                
                var notes = await _context.Notes
                    .Include(n => n.Owner)
                    .Where(n => 
                        n.IsPublic && 
                        n.Status == NoteStatus.Public
                    )
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Found {notes.Count} public notes");
                
                foreach (var note in notes)
                {
                    _logger.LogInformation($"Public note: Id={note.Id}, Title={note.Title}, " +
                        $"IsPublic={note.IsPublic}, Status={note.Status}, OwnerId={note.OwnerId}");
                }
                
                return notes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public notes for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Note>> GetSharedNotesAsync(int userId)
        {
            return await _context.Notes
                .Include(n => n.Owner)
                .Include(n => n.NoteShares)
                .Where(n => n.NoteShares.Any(s => s.UserId == userId))
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task ShareNoteAsync(int noteId, int collaboratorId)
        {
            try
            {
                _logger.LogInformation($"Sharing note {noteId} with user {collaboratorId}");
                
                // Check if note exists
                var note = await _context.Notes.FindAsync(noteId);
                if (note == null)
                    throw new NotFoundException($"Note with id {noteId} not found");

                // Check if share already exists
                var existingShare = await _context.NoteShares
                    .FirstOrDefaultAsync(s => s.NoteId == noteId && s.UserId == collaboratorId);
                    
                if (existingShare != null)
                {
                    _logger.LogInformation($"Note {noteId} is already shared with user {collaboratorId}");
                    return;
                }

                // Create new share record
                var noteShare = new NoteShare
                {
                    NoteId = noteId,
                    UserId = collaboratorId,
                    SharedAt = DateTime.UtcNow
                };

                _context.NoteShares.Add(noteShare);
                
                // Update note status
                note.Status = NoteStatus.Shared;
                note.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Note {noteId} shared successfully with user {collaboratorId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sharing note {noteId} with user {collaboratorId}");
                throw;
            }
        }

        public async Task MakeNotePublicAsync(int noteId)
        {
            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
            {
                throw new KeyNotFoundException($"Note with id {noteId} not found");
            }

            _logger.LogInformation($"Making note {noteId} public. Current status: {note.Status}, IsPublic: {note.IsPublic}");
            
            note.IsPublic = true;
            note.Status = NoteStatus.Public;  // This should set it to 2
            note.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Note {noteId} is now public. New status: {note.Status}, IsPublic: {note.IsPublic}");
        }
    }
}