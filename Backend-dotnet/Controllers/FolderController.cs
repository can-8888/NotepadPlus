using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NotepadPlusApi.Models;
using NotepadPlusApi.Services.Interfaces;
using NotepadPlusApi.Models.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using NotepadPlusApi.Exceptions;

namespace NotepadPlusApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/drive")]
    public class FolderController : ControllerBase
    {
        private readonly IFolderService _folderService;
        private readonly ILogger<FolderController> _logger;
        private readonly ApplicationDbContext _context;

        public FolderController(IFolderService folderService, ILogger<FolderController> logger, ApplicationDbContext context)
        {
            _folderService = folderService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("folders")]
        public async Task<ActionResult<Folder>> CreateFolder([FromBody] CreateFolderRequest request)
        {
            try
            {
                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var folder = await _folderService.CreateFolderAsync(
                    name: request.Name,
                    userId: userId,
                    parentFolderId: request.ParentFolderId
                );

                return Ok(new { data = folder });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder");
                return StatusCode(500, new { message = "Error creating folder" });
            }
        }

        [HttpGet("folders")]
        public async Task<ActionResult<IEnumerable<Folder>>> GetFolders()
        {
            try
            {
                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in token");
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var folders = await _folderService.GetUserFoldersAsync(userId);
                _logger.LogInformation($"Retrieved {folders.Count()} folders for user {userId}");
                
                // Ensure we're returning an array in the data property
                return Ok(new { 
                    data = folders ?? new List<Folder>(),  // Return empty array if null
                    success = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFolders");
                return StatusCode(500, new { 
                    message = "Error getting folders",
                    error = ex.Message,
                    success = false
                });
            }
        }

        [HttpGet("folders/root/files")]
        public async Task<ActionResult<IEnumerable<DriveFile>>> GetRootFiles()
        {
            try
            {
                // Log the request headers
                _logger.LogInformation("Auth header: {auth}", Request.Headers["Authorization"].ToString());
                _logger.LogInformation("User claims: {claims}", string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}")));

                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in token");
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                _logger.LogInformation($"Getting root files for user {userId}");

                // Check database connection
                try
                {
                    await _context.Database.CanConnectAsync();
                    _logger.LogInformation("Database connection successful");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database connection failed");
                    return StatusCode(500, new { 
                        message = "Database connection error",
                        error = ex.Message,
                        details = ex.InnerException?.Message 
                    });
                }

                var files = await _folderService.GetFilesInFolderAsync(null, userId);
                _logger.LogInformation($"Retrieved {files.Count()} root files for user {userId}");
                
                return Ok(new { data = files });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found error in GetRootFiles");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root files");
                return StatusCode(500, new { 
                    message = "Error getting root files",
                    error = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("folders/{folderId}/files")]
        public async Task<ActionResult<IEnumerable<DriveFile>>> GetFilesInFolder(int folderId)
        {
            try
            {
                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var files = await _folderService.GetFilesInFolderAsync(folderId, userId);
                return Ok(new { data = files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files in folder");
                return StatusCode(500, new { message = "Error getting files" });
            }
        }

        [HttpDelete("folders/{folderId}")]
        public async Task<ActionResult> DeleteFolder(int folderId)
        {
            try
            {
                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                var result = await _folderService.DeleteFolderAsync(folderId, userId);
                if (!result)
                {
                    return NotFound(new { message = "Folder not found or access denied" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder");
                return StatusCode(500, new { message = "Error deleting folder" });
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)]  // 100MB limit
        public async Task<ActionResult<DriveFile>> UploadFile([FromForm] IFormFile file, [FromForm] int? folderId = null)
        {
            try
            {
                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file was uploaded" });
                }

                // Create unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var contentType = file.ContentType;
                var filePath = Path.Combine("uploads", fileName);  // Make sure this directory exists

                // Ensure directory exists
                Directory.CreateDirectory("uploads");

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create database record
                var driveFile = new DriveFile
                {
                    Name = file.FileName,
                    ContentType = contentType,
                    Size = file.Length,
                    Path = filePath,
                    OwnerId = userId,
                    FolderId = folderId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.DriveFiles.Add(driveFile);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"File {driveFile.Name} uploaded successfully by user {userId}");
                return Ok(new { data = driveFile });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
            }
        }

        [HttpDelete("files/{id}")]
        public async Task<ActionResult> DeleteFile(int id)
        {
            try
            {
                if (!int.TryParse(User.FindFirst("id")?.Value, out int userId))
                {
                    return Unauthorized(new { message = "Invalid user ID" });
                }

                // First check if file exists and belongs to user
                var file = await _context.DriveFiles.FindAsync(id);
                if (file == null)
                {
                    return NotFound(new { message = "File not found" });
                }

                if (file.OwnerId != userId)
                {
                    return Unauthorized(new { message = "Access denied" });
                }

                // Delete the physical file if it exists
                if (System.IO.File.Exists(file.Path))
                {
                    try
                    {
                        System.IO.File.Delete(file.Path);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting physical file");
                        // Continue with database deletion even if physical file deletion fails
                    }
                }

                _context.DriveFiles.Remove(file);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return StatusCode(500, new { message = "Error deleting file" });
            }
        }
    }

    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
    }
} 