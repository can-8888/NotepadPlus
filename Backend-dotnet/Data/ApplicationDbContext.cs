using Microsoft.EntityFrameworkCore;
using NotepadPlusApi.Models;

namespace NotepadPlusApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<Folder> Folders { get; set; } = null!;
    public DbSet<NoteShare> NoteShares { get; set; } = null!;
    public DbSet<NotePermission> NotePermissions { get; set; } = null!;
    public DbSet<UserToken> UserTokens { get; set; } = null!;
    public DbSet<DriveFile> DriveFiles { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>()
            .HasOne(n => n.Owner)
            .WithMany()
            .HasForeignKey(n => n.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Note>()
            .HasMany(n => n.Collaborators)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "NoteCollaborators",
                j => j
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.NoAction),
                j => j
                    .HasOne<Note>()
                    .WithMany()
                    .HasForeignKey("NoteId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("NoteId", "UserId");
                    j.ToTable("NoteCollaborators");
                }
            );

        modelBuilder.Entity<Note>()
            .HasMany(n => n.NoteShares)
            .WithOne(ns => ns.Note)
            .HasForeignKey(ns => ns.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Note>()
            .HasMany(n => n.Notifications)
            .WithOne(n => n.Note)
            .HasForeignKey(n => n.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NoteShare>()
            .HasOne(ns => ns.User)
            .WithMany()
            .HasForeignKey(ns => ns.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<NotePermission>()
            .HasOne(np => np.Note)
            .WithMany(n => n.Permissions)
            .HasForeignKey(np => np.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotePermission>()
            .HasOne(np => np.User)
            .WithMany()
            .HasForeignKey(np => np.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<NotePermission>()
            .HasIndex(np => new { np.NoteId, np.UserId })
            .IsUnique();

        modelBuilder.Entity<Folder>()
            .HasOne(f => f.ParentFolder)
            .WithMany(f => f.SubFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Folder>()
            .HasOne(f => f.Owner)
            .WithMany()
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DriveFile>()
            .HasOne(f => f.Owner)
            .WithMany()
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DriveFile>()
            .HasOne(f => f.Folder)
            .WithMany(f => f.Files)
            .HasForeignKey(f => f.FolderId)
            .OnDelete(DeleteBehavior.NoAction);

        // Initialize collections
        modelBuilder.Entity<Folder>()
            .Property(f => f.Name)
            .IsRequired();

        modelBuilder.Entity<UserToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserToken>()
            .Property(t => t.Token)
            .IsRequired();
    }
}