using System.Collections.Generic;
using System.Threading.Tasks;
using NotepadPlusApi.Models;

namespace NotepadPlusApi.Services.Interfaces
{
    public interface IFolderService
    {
        Task<IEnumerable<Folder>> GetUserFoldersAsync(int userId);
        Task<IEnumerable<Folder>> GetFoldersAsync(int userId);
        Task<Folder> CreateFolderAsync(string name, int userId, int? parentFolderId = null);
        Task<Folder> UpdateFolderAsync(int id, string name, int? parentFolderId);
        Task<bool> DeleteFolderAsync(int id, int userId);
        Task<Folder> GetFolderAsync(int id, int userId);
        Task<bool> ValidateFolderAccessAsync(int folderId, int userId);
        Task<bool> DeleteFolderContentsAsync(int folderId, int userId);
        Task<IEnumerable<DriveFile>> GetFilesInFolderAsync(int? folderId, int userId);
    }
} 