using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Tests;

public class DatabaseModelTests
{
    /// <summary>Creates an empty in-memory SQLite database with the real schema.</summary>
    private static (SqliteConnection Connection, AppDbContext Db) CreateDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (connection, db);
    }

    [Fact]
    public void Model_HasSixTables()
    {
        var (connection, db) = CreateDatabase();
        using var _ = connection;
        using var __ = db;

        var tables = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .OrderBy(name => name)
            .ToList();

        Assert.Equal(new[] { "Categories", "Comments", "Priorities", "TicketLogs", "Tickets", "Users" }, tables);
    }

    [Fact]
    public void Ticket_CanBeSavedWithAllRelations()
    {
        var (connection, db) = CreateDatabase();
        using var _ = connection;
        using var __ = db;

        var now = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var author = new User { Email = "user@example.com", DisplayName = "User", PasswordHash = "x", Role = UserRole.User, CreatedAt = now };
        var support = new User { Email = "support@example.com", DisplayName = "Support", PasswordHash = "x", Role = UserRole.Support, CreatedAt = now };
        var category = new Category { Name = "Network" };
        var priority = new Priority { Name = "High", Level = 3, DeadlineHours = 8 };

        var ticket = new Ticket
        {
            Title = "Printer does not work",
            Description = "The printer on the second floor shows an error.",
            Category = category,
            Priority = priority,
            CreatedBy = author,
            Assignee = support,
            Status = TicketStatus.InProgress,
            CreatedAt = now,
            Deadline = now.AddHours(priority.DeadlineHours)
        };
        ticket.Comments.Add(new Comment { Author = support, Text = "Looking into it.", CreatedAt = now });
        ticket.Logs.Add(new TicketLog { User = support, Action = TicketLogActions.Assigned, NewValue = "Support", CreatedAt = now });

        db.Tickets.Add(ticket);
        db.SaveChanges();
        db.ChangeTracker.Clear();

        var saved = db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.CreatedBy)
            .Include(t => t.Assignee)
            .Include(t => t.Comments)
            .Include(t => t.Logs)
            .Single();

        Assert.Equal("Network", saved.Category.Name);
        Assert.Equal(8, saved.Priority.DeadlineHours);
        Assert.Equal("user@example.com", saved.CreatedBy.Email);
        Assert.Equal("support@example.com", saved.Assignee!.Email);
        Assert.Equal(TicketStatus.InProgress, saved.Status);
        Assert.Single(saved.Comments);
        Assert.Single(saved.Logs);
    }
}
