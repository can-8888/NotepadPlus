namespace NotepadPlusApi.Models;
using System.ComponentModel.DataAnnotations;

public class NoteDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Category { get; set; }

    public int UserId { get; set; }
} 