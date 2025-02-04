using System;

namespace NotepadPlusApi.Models
{
    public class DriveFile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Path { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public int? FolderId { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsPublic { get; set; }
        
        // Navigation properties
        public User Owner { get; set; } = null!;
        public Folder? Folder { get; set; }
    }
} 