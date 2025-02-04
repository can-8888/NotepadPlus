using System;
using System.Collections.Generic;

namespace NotepadPlusApi.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public int? ParentFolderId { get; set; }
        public Folder? ParentFolder { get; set; }
        public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public ICollection<DriveFile> Files { get; set; } = new List<DriveFile>();
    }
} 