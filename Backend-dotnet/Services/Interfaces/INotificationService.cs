using NotepadPlusApi.Models;

namespace NotepadPlusApi.Services.Interfaces;

public interface INotificationService
{
    Task CreatePublicNoteNotification(Note note);
    Task CreateNoteSharedNotification(Note note, int sharedWithUserId);
} 