using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class TicketEscalationTests
{
    private static readonly DateTime Start = new(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>A clock that the test can move forward, so tickets become late.</summary>
    private sealed class TestClock : TimeProvider
    {
        public DateTime Now { get; set; } = Start;
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private sealed record Context(SqliteConnection Connection, AppDbContext Db, TicketService Service, TestClock Clock)
        : IDisposable
    {
        public void Dispose() => Connection.Dispose();
    }

    /// <summary>A database with the seeded users and priorities but no tickets.</summary>
    private static Context CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        SeedData.Initialize(db);
        db.Tickets.RemoveRange(db.Tickets.ToList());
        db.SaveChanges();

        var clock = new TestClock();
        var service = new TicketService(
            new TicketRepository(db), new CategoryRepository(db), new PriorityRepository(db),
            new UserRepository(db), new TicketLogRepository(db), new UnitOfWork(db), new NullTicketNotifier(),
            clock, NullLogger<TicketService>.Instance);
        return new Context(connection, db, service, clock);
    }

    private static int UserId(Context ctx, string email) => ctx.Db.Users.Single(u => u.Email == email).Id;

    private static async Task<TicketDto> NewTicketAsync(Context ctx, string priority) =>
        await ctx.Service.CreateAsync(new CreateTicketRequest
        {
            Title = $"Tiket ({priority})",
            Description = "Kirjeldus on piisavalt pikk.",
            CategoryId = ctx.Db.Categories.First().Id,
            PriorityId = ctx.Db.Priorities.Single(p => p.Name == priority).Id
        }, UserId(ctx, SeedData.User1Email));

    [Fact]
    public async Task GetOverdue_ReturnsOnlyUnresolvedTicketsPastTheirDeadline_OldestDeadlineFirst()
    {
        using var ctx = CreateContext();
        await NewTicketAsync(ctx, "Madal");                     // deadline after 72 h
        var high = await NewTicketAsync(ctx, "Kõrge");          // deadline after 8 h
        var critical = await NewTicketAsync(ctx, "Kriitiline"); // deadline after 4 h
        var resolvedCritical = await NewTicketAsync(ctx, "Kriitiline");
        ctx.Db.Tickets.Single(t => t.Id == resolvedCritical.Id).Status = TicketStatus.Resolved;
        ctx.Db.SaveChanges();

        ctx.Clock.Now = Start.AddHours(5);
        var afterFiveHours = await ctx.Service.GetOverdueAsync();
        ctx.Clock.Now = Start.AddHours(9);
        var afterNineHours = await ctx.Service.GetOverdueAsync();

        Assert.Equal(new[] { critical.Id }, afterFiveHours.Select(t => t.Id).ToArray());
        Assert.Equal(new[] { critical.Id, high.Id }, afterNineHours.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task Escalate_RaisesPriorityMarksOverdueKeepsDeadlineAndWritesLog()
    {
        using var ctx = CreateContext();
        var service = UserId(ctx, SeedData.ServiceEmail);
        var ticket = await NewTicketAsync(ctx, "Madal");
        ctx.Clock.Now = Start.AddHours(73);

        var result = await ctx.Service.EscalateAsync(ticket.Id, service);
        var log = await ctx.Service.GetLogAsync(ticket.Id, service, UserRole.Service);

        Assert.True(result.IsOverdue);
        Assert.Equal("Keskmine", result.Priority.Name);
        Assert.Equal(ticket.Deadline, result.Deadline);
        var entry = log[^1];
        Assert.Equal(TicketLogActions.Escalated, entry.Action);
        Assert.Equal("Madal", entry.OldValue);
        Assert.Equal("Keskmine", entry.NewValue);
        Assert.Equal(service, entry.User.Id);
    }

    [Fact]
    public async Task Escalate_HighestPriority_IsOnlyMarkedOverdue()
    {
        using var ctx = CreateContext();
        var ticket = await NewTicketAsync(ctx, "Kriitiline");
        ctx.Clock.Now = Start.AddHours(5);

        var result = await ctx.Service.EscalateAsync(ticket.Id, UserId(ctx, SeedData.ServiceEmail));

        Assert.True(result.IsOverdue);
        Assert.Equal("Kriitiline", result.Priority.Name);
    }

    [Fact]
    public async Task Escalate_NotLateResolvedOrAlreadyEscalated_ThrowsConflict_UnknownIsNotFound()
    {
        using var ctx = CreateContext();
        var service = UserId(ctx, SeedData.ServiceEmail);
        var late = await NewTicketAsync(ctx, "Kriitiline");
        var resolved = await NewTicketAsync(ctx, "Kriitiline");
        ctx.Db.Tickets.Single(t => t.Id == resolved.Id).Status = TicketStatus.Resolved;
        ctx.Db.SaveChanges();

        // The deadline has not passed yet.
        await Assert.ThrowsAsync<ConflictException>(() => ctx.Service.EscalateAsync(late.Id, service));

        ctx.Clock.Now = Start.AddHours(5);
        await ctx.Service.EscalateAsync(late.Id, service);

        // Already escalated, resolved, and missing.
        await Assert.ThrowsAsync<ConflictException>(() => ctx.Service.EscalateAsync(late.Id, service));
        await Assert.ThrowsAsync<ConflictException>(() => ctx.Service.EscalateAsync(resolved.Id, service));
        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service.EscalateAsync(9999, service));
    }
}
