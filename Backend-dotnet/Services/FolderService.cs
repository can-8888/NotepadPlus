using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using NotepadPlusApi.Models;
using NotepadPlusApi.Services.Interfaces;
using NotepadPlusApi.Exceptions;
using Microsoft.Extensions.Logging;

namespace NotepadPlusApi.Services
{
    public class FolderService : IFolderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FolderService> _logger;

        public FolderService(ApplicationDbContext context, ILogger<FolderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Folder>> GetUserFoldersAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Getting folders for user {userId}");

                // First check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User {userId} not found");
                    throw new NotFoundException($"User {userId} not found");
                }

                // Debug: Check database connection
                try
                {
                    await _context.Database.CanConnectAsync();
                    _logger.LogInformation("Database connection successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database connection failed");
                    throw;
                }

                // Debug: Print SQL query
                var query = _context.Folders
                    .Where(f => f.OwnerId == userId)
                    .Include(f => f.Files)
                    .OrderBy(f => f.Name);

                _logger.LogInformation($"Executing query: {query.ToQueryString()}");

                var folders = await query.ToListAsync();
                _logger.LogInformation($"Found {folders.Count} folders for user {userId}");

                return folders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting folders for user {userId}");
                throw new Exception($"Error getting folders: {ex.Message}", ex);
            }
        }

        public async Task<Folder> CreateFolderAsync(string name, int userId, int? parentFolderId = null)
        {
            var folder = new Folder
            {
                Name = name,
                OwnerId = userId,
                ParentFolderId = parentFolderId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

        public async Task<Folder> UpdateFolderAsync(int id, string name, int? parentFolderId)
        {
            var folder = await _context.Folders.FindAsync(id);
            if (folder == null)
                throw new KeyNotFoundException($"Folder with id {id} not found");

            folder.Name = name;
            folder.ParentFolderId = parentFolderId;

            await _context.SaveChangesAsync();
            return folder;
        }

        public async Task<bool> DeleteFolderAsync(int id, int userId)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId);

            if (folder == null)
                return false;

            // Delete all files in the folder
            if (folder.Files.Any())
            {
                _context.DriveFiles.RemoveRange(folder.Files);
            }

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Folder> GetFolderAsync(int id, int userId)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .Include(f => f.SubFolders)
                .FirstOrDefaultAsync(f => f.Id == id && f.OwnerId == userId);

            if (folder == null)
                throw new NotFoundException($"Folder with id {id} not found");

            return folder;
        }

        public async Task<bool> ValidateFolderAccessAsync(int folderId, int userId)
        {
            var folder = await _context.Folders
                .FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == userId);
            return folder != null;
        }

        public async Task<bool> DeleteFolderContentsAsync(int folderId, int userId)
        {
            var folder = await _context.Folders
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == folderId && f.OwnerId == userId);

            if (folder == null)
                return false;

            // Delete all files in the folder
            if (folder.Files.Any())
            {
                _context.DriveFiles.RemoveRange(folder.Files);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Folder>> GetFoldersAsync(int userId)
        {
            try
            {
                return await _context.Folders
                    .Include(f => f.Files)
                    .Where(f => f.OwnerId == userId && f.ParentFolderId == null)
                    .OrderBy(f => f.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for user {UserId}", userId);
                throw;
            }
        }

        private async Task DeleteSubFoldersAsync(Folder folder)
        {
            foreach (var subFolder in folder.SubFolders)
            {
                await DeleteFolderAsync(subFolder.Id, folder.OwnerId);
            }
        }

        public async Task<IEnumerable<DriveFile>> GetFilesInFolderAsync(int? folderId, int userId)
        {
            try
            {
                _logger.LogInformation($"Getting files for user {userId} in folder {folderId}");

                // First check if user exists
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User {userId} not found");
                    throw new NotFoundException($"User {userId} not found");
                }

                var query = _context.DriveFiles.Where(f => f.OwnerId == userId);
                
                if (folderId.HasValue)
                {
                    // If folder specified, verify it exists and belongs to user
                    var folder = await _context.Folders.FindAsync(folderId.Value);
                    if (folder == null || folder.OwnerId != userId)
                    {
                        _logger.LogWarning($"Folder {folderId} not found or access denied");
                        throw new NotFoundException($"Folder not found or access denied");
                    }
                    query = query.Where(f => f.FolderId == folderId);
                }
                else
                {
                    query = query.Where(f => f.FolderId == null);
                }

                var files = await query.OrderByDescending(f => f.UploadedAt).ToListAsync();
                _logger.LogInformation($"Found {files.Count} files");
                
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting files for user {userId} in folder {folderId}");
                throw;
            }
        }
    }
} 