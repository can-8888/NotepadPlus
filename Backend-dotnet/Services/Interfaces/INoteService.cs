using System.Collections.Generic;
using System.Threading.Tasks;
using NotepadPlusApi.Models;

namespace NotepadPlusApi.Services.Interfaces
{
    public interface INoteService
    {
        Task<IEnumerable<Note>> GetNotesAsync(int userId);
        Task<Note?> GetNoteByIdAsync(int id);
        Task<Note> CreateNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(int id, Note note);
        Task DeleteNoteAsync(int id);
        Task<IEnumerable<Note>> GetPublicNotesAsync(int userId);
        Task<IEnumerable<Note>> GetSharedNotesAsync(int userId);
        Task ShareNoteAsync(int noteId, int collaboratorId);
        Task MakeNotePublicAsync(int noteId);
    }
} 