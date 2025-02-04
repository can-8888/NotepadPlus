using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NotepadPlusApi.Models;

public class Note
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? Category { get; set; }

    [Required]
    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<User> Collaborators { get; set; } = new List<User>();

    [JsonIgnore]
    public virtual ICollection<NoteShare> NoteShares { get; set; } = new List<NoteShare>();

    public virtual ICollection<NotePermission> Permissions { get; set; } = new List<NotePermission>();

    [NotMapped]
    public List<int> SharedWithIds { get; set; } = new List<int>();

    // Add validation for dates
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsPublic { get; set; }
    public NoteStatus Status { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}