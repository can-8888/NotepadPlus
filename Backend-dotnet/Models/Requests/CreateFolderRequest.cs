namespace NotepadPlusApi.Models.Requests
{
    public class CreateFolderRequest
    {
        public required string Name { get; set; }
        public int? ParentId { get; set; }
    }
} 