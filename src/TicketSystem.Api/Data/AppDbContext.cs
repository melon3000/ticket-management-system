using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Data;

/// <summary>Entity Framework Core context. Only the API talks to the database.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<TicketLog> TicketLogs => Set<TicketLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).IsRequired().HasMaxLength(255);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Priority>(e =>
        {
            e.Property(p => p.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(p => p.Name).IsUnique();
            e.HasIndex(p => p.Level).IsUnique();
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.Property(t => t.Title).IsRequired().HasMaxLength(100);
            e.Property(t => t.Description).IsRequired().HasMaxLength(2000);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

            // Indexes for the filters and for the overdue check.
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.AssigneeId);
            e.HasIndex(t => t.Deadline);

            // Reference data and users are never deleted together with their tickets.
            e.HasOne(t => t.Category).WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Priority).WithMany(p => p.Tickets)
                .HasForeignKey(t => t.PriorityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CreatedBy).WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Assignee).WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(e =>
        {
            e.Property(c => c.Text).IsRequired().HasMaxLength(2000);

            // Deleting a ticket also deletes its comments.
            e.HasOne(c => c.Ticket).WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Author).WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketLog>(e =>
        {
            e.Property(l => l.Action).IsRequired().HasMaxLength(50);
            e.Property(l => l.OldValue).HasMaxLength(200);
            e.Property(l => l.NewValue).HasMaxLength(200);

            // Deleting a ticket also deletes its change log.
            e.HasOne(l => l.Ticket).WithMany(t => t.Logs)
                .HasForeignKey(l => l.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.User).WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
