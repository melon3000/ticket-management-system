using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;

namespace TicketSystem.Tests;

/// <summary>Creates the in-memory SQLite databases that the service and repository tests share.</summary>
internal static class TestDatabase
{
    /// <summary>An open connection and a context with the empty schema. The caller disposes the connection.</summary>
    public static (SqliteConnection Connection, AppDbContext Db) CreateEmpty()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return (connection, db);
    }

    /// <summary>
    /// A database with the seeded users, categories, priorities and example tickets.
    /// Pass <paramref name="withTickets"/> as false to remove the example tickets and their comments and log rows.
    /// </summary>
    public static (SqliteConnection Connection, AppDbContext Db) CreateSeeded(bool withTickets = true)
    {
        var (connection, db) = CreateEmpty();
        SeedData.Initialize(db);
        if (!withTickets)
        {
            db.Tickets.RemoveRange(db.Tickets.ToList());
            db.SaveChanges();
        }
        return (connection, db);
    }
}
