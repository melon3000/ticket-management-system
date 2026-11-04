using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Tests;

public class SeedDataTests
{
    /// <summary>Creates an in-memory SQLite database from the real migrations and fills it with the starting data.</summary>
    private static (SqliteConnection Connection, AppDbContext Db) CreateSeededDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.Migrate();
        SeedData.Initialize(db);
        return (connection, db);
    }

    [Fact]
    public void Seed_CreatesReferenceDataAndAllRoles()
    {
        var (connection, db) = CreateSeededDatabase();
        using var _ = connection;
        using var __ = db;

        Assert.Equal(5, db.Categories.Count());
        Assert.Equal(4, db.Priorities.Count());
        Assert.Equal(6, db.Users.Count());
        Assert.Equal(2, db.Users.Count(u => u.Role == UserRole.User));
        Assert.Equal(2, db.Users.Count(u => u.Role == UserRole.Support));
        Assert.Equal(1, db.Users.Count(u => u.Role == UserRole.Admin));
        Assert.Equal(1, db.Users.Count(u => u.Role == UserRole.Service));
    }

    [Fact]
    public void Seed_CreatesTicketsWithOneOverdueAndSomeResolved()
    {
        var (connection, db) = CreateSeededDatabase();
        using var _ = connection;
        using var __ = db;

        var now = DateTime.UtcNow;
        Assert.True(db.Tickets.Count() >= 10);

        var resolved = db.Tickets.Where(t => t.Status == TicketStatus.Resolved).ToList();
        Assert.True(resolved.Count >= 5);
        Assert.All(resolved, t => Assert.NotNull(t.ResolvedAt));

        // Exactly one unresolved ticket is past its deadline and not yet escalated.
        var overdue = db.Tickets.Where(t => t.Status != TicketStatus.Resolved && t.Deadline < now && !t.IsOverdue).ToList();
        Assert.Single(overdue);

        Assert.True(db.TicketLogs.Any());
        Assert.True(db.Comments.Any());
    }

    [Fact]
    public void Seed_CanRunTwiceWithoutDuplicates()
    {
        var (connection, db) = CreateSeededDatabase();
        using var _ = connection;
        using var __ = db;

        var usersBefore = db.Users.Count();
        var ticketsBefore = db.Tickets.Count();

        SeedData.Initialize(db);

        Assert.Equal(usersBefore, db.Users.Count());
        Assert.Equal(ticketsBefore, db.Tickets.Count());
    }

    [Fact]
    public void Seed_StoresHashedPasswordsThatCanBeVerified()
    {
        var (connection, db) = CreateSeededDatabase();
        using var _ = connection;
        using var __ = db;

        var admin = db.Users.Single(u => u.Email == SeedData.AdminEmail);
        Assert.NotEqual(SeedData.DevPassword, admin.PasswordHash);

        var hasher = new PasswordHasher<User>();
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(admin, admin.PasswordHash, SeedData.DevPassword));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(admin, admin.PasswordHash, "wrong"));
    }
}
