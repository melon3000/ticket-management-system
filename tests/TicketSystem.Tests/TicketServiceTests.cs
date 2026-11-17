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

public class TicketServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>A clock that always returns the same moment, so deadlines can be checked exactly.</summary>
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(FixedNow);
    }

    private sealed record Context(SqliteConnection Connection, AppDbContext Db, TicketService Service) : IDisposable
    {
        public void Dispose() => Connection.Dispose();
    }

    private static Context CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        SeedData.Initialize(db);

        var service = new TicketService(
            new TicketRepository(db), new CategoryRepository(db), new PriorityRepository(db),
            new UserRepository(db), new TicketLogRepository(db), new UnitOfWork(db), new NullTicketNotifier(),
            new FixedClock(), NullLogger<TicketService>.Instance);
        return new Context(connection, db, service);
    }

    private static int UserId(AppDbContext db, string email) => db.Users.Single(u => u.Email == email).Id;
    private static int PriorityId(AppDbContext db, string name) => db.Priorities.Single(p => p.Name == name).Id;
    private static int CategoryId(AppDbContext db) => db.Categories.First().Id;

    private static CreateTicketRequest NewRequest(AppDbContext db, string priority = "Kõrge") => new()
    {
        Title = "Projektor ei käivitu",
        Description = "Klassiruumi projektor ei lülitu sisse.",
        CategoryId = CategoryId(db),
        PriorityId = PriorityId(db, priority)
    };

    [Fact]
    public async Task Create_SetsDeadlineFromPriorityAndWritesCreatedLog()
    {
        using var ctx = CreateContext();
        var userId = UserId(ctx.Db, SeedData.User1Email);

        var ticket = await ctx.Service.CreateAsync(NewRequest(ctx.Db, "Kõrge"), userId);

        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.Equal(FixedNow, ticket.CreatedAt);
        Assert.Equal(FixedNow.AddHours(8), ticket.Deadline);
        var log = Assert.Single(ctx.Db.TicketLogs.Where(l => l.TicketId == ticket.Id));
        Assert.Equal(TicketLogActions.Created, log.Action);
        Assert.Equal(userId, log.UserId);
    }

    [Fact]
    public async Task Create_WithUnknownCategory_ThrowsBadRequest()
    {
        using var ctx = CreateContext();
        var request = NewRequest(ctx.Db);
        request.CategoryId = 9999;

        await Assert.ThrowsAsync<BadRequestException>(
            () => ctx.Service.CreateAsync(request, UserId(ctx.Db, SeedData.User1Email)));
    }

    [Fact]
    public async Task Get_AsOtherOrdinaryUser_ThrowsForbiddenButSupportCanRead()
    {
        using var ctx = CreateContext();
        var author = UserId(ctx.Db, SeedData.User1Email);
        var other = UserId(ctx.Db, SeedData.User2Email);
        var support = UserId(ctx.Db, SeedData.Support1Email);
        var ticket = await ctx.Service.CreateAsync(NewRequest(ctx.Db), author);

        await Assert.ThrowsAsync<ForbiddenException>(() => ctx.Service.GetAsync(ticket.Id, other, UserRole.User));
        var seen = await ctx.Service.GetAsync(ticket.Id, support, UserRole.Support);
        Assert.Equal(ticket.Id, seen.Id);
    }

    [Fact]
    public async Task Update_ChangingPriority_RecalculatesDeadlineFromCreationTime()
    {
        using var ctx = CreateContext();
        var author = UserId(ctx.Db, SeedData.User1Email);
        var ticket = await ctx.Service.CreateAsync(NewRequest(ctx.Db, "Madal"), author);

        var update = new UpdateTicketRequest
        {
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.Category.Id,
            PriorityId = PriorityId(ctx.Db, "Kriitiline")
        };
        var updated = await ctx.Service.UpdateAsync(ticket.Id, update, author, UserRole.User);

        Assert.Equal("Kriitiline", updated.Priority.Name);
        Assert.Equal(FixedNow.AddHours(4), updated.Deadline);
    }

    [Fact]
    public async Task Update_ResolvedTicket_ThrowsConflict()
    {
        using var ctx = CreateContext();
        var resolved = ctx.Db.Tickets.First(t => t.Status == TicketStatus.Resolved);
        var update = new UpdateTicketRequest
        {
            Title = "Uus pealkiri",
            Description = "Uus kirjeldus tiketile.",
            CategoryId = resolved.CategoryId,
            PriorityId = resolved.PriorityId
        };

        await Assert.ThrowsAsync<ConflictException>(() =>
            ctx.Service.UpdateAsync(resolved.Id, update, UserId(ctx.Db, SeedData.AdminEmail), UserRole.Admin));
    }

    [Fact]
    public async Task Delete_RemovesTicketAndMissingTicketThrowsNotFound()
    {
        using var ctx = CreateContext();
        var ticket = await ctx.Service.CreateAsync(NewRequest(ctx.Db), UserId(ctx.Db, SeedData.User1Email));

        await ctx.Service.DeleteAsync(ticket.Id);

        Assert.False(ctx.Db.Tickets.Any(t => t.Id == ticket.Id));
        Assert.False(ctx.Db.TicketLogs.Any(l => l.TicketId == ticket.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service.DeleteAsync(ticket.Id));
    }
}
