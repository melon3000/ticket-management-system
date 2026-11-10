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

public class TicketWorkflowTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

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
            new UserRepository(db), new TicketLogRepository(db), new UnitOfWork(db),
            new FixedClock(), NullLogger<TicketService>.Instance);
        return new Context(connection, db, service);
    }

    private static User UserByEmail(Context ctx, string email) => ctx.Db.Users.Single(u => u.Email == email);

    /// <summary>Creates a new ticket written by the given user.</summary>
    private static async Task<TicketDto> NewTicketAsync(Context ctx, string authorEmail = SeedData.User1Email) =>
        await ctx.Service.CreateAsync(new CreateTicketRequest
        {
            Title = "Projektor ei käivitu",
            Description = "Klassiruumi projektor ei lülitu sisse.",
            CategoryId = ctx.Db.Categories.First().Id,
            PriorityId = ctx.Db.Priorities.First().Id
        }, UserByEmail(ctx, authorEmail).Id);

    [Fact]
    public async Task Assign_TakesTicketIntoWorkAndWritesLog()
    {
        using var ctx = CreateContext();
        var support = UserByEmail(ctx, SeedData.Support1Email);
        var ticket = await NewTicketAsync(ctx);

        var result = await ctx.Service.AssignAsync(ticket.Id, support.Id);
        var log = await ctx.Service.GetLogAsync(ticket.Id, support.Id, UserRole.Support);

        Assert.Equal(TicketStatus.InProgress, result.Status);
        Assert.Equal(support.Id, result.Assignee!.Id);
        Assert.Equal(new[] { "Created", "Assigned", "StatusChanged" }, log.Select(l => l.Action).ToArray());
        Assert.Equal(support.DisplayName, log[1].NewValue);
        Assert.Equal(support.Id, log[1].User.Id);
        Assert.Equal("New", log[2].OldValue);
        Assert.Equal("InProgress", log[2].NewValue);
    }

    [Fact]
    public async Task Assign_TicketAlreadyTaken_ThrowsConflict()
    {
        using var ctx = CreateContext();
        var ticket = await NewTicketAsync(ctx);
        await ctx.Service.AssignAsync(ticket.Id, UserByEmail(ctx, SeedData.Support1Email).Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => ctx.Service.AssignAsync(ticket.Id, UserByEmail(ctx, SeedData.Support2Email).Id));
        await Assert.ThrowsAsync<NotFoundException>(
            () => ctx.Service.AssignAsync(9999, UserByEmail(ctx, SeedData.Support2Email).Id));
    }

    [Fact]
    public async Task ChangeStatus_ResolvesAndReopens_SettingAndClearingResolvedAt()
    {
        using var ctx = CreateContext();
        var support = UserByEmail(ctx, SeedData.Support1Email);
        var ticket = await NewTicketAsync(ctx);
        await ctx.Service.AssignAsync(ticket.Id, support.Id);

        var resolved = await ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, support.Id, UserRole.Support);
        var reopened = await ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress, support.Id, UserRole.Support);
        var log = await ctx.Service.GetLogAsync(ticket.Id, support.Id, UserRole.Support);

        Assert.Equal(TicketStatus.Resolved, resolved.Status);
        Assert.Equal(FixedNow, resolved.ResolvedAt);
        Assert.Equal(TicketStatus.InProgress, reopened.Status);
        Assert.Null(reopened.ResolvedAt);
        Assert.Equal(3, log.Count(l => l.Action == TicketLogActions.StatusChanged));
        Assert.Equal("Resolved", log[^1].OldValue);
        Assert.Equal("InProgress", log[^1].NewValue);
    }

    [Fact]
    public async Task ChangeStatus_OtherSupportPersonIsForbidden_AdministratorIsAllowed()
    {
        using var ctx = CreateContext();
        var ticket = await NewTicketAsync(ctx);
        await ctx.Service.AssignAsync(ticket.Id, UserByEmail(ctx, SeedData.Support1Email).Id);
        var otherSupport = UserByEmail(ctx, SeedData.Support2Email);
        var admin = UserByEmail(ctx, SeedData.AdminEmail);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, otherSupport.Id, UserRole.Support));
        var result = await ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, admin.Id, UserRole.Admin);
        var log = await ctx.Service.GetLogAsync(ticket.Id, admin.Id, UserRole.Admin);

        Assert.Equal(TicketStatus.Resolved, result.Status);
        Assert.Equal(admin.Id, log[^1].User.Id);
    }

    [Fact]
    public async Task ChangeStatus_NotAllowedTransitions_ThrowConflict()
    {
        using var ctx = CreateContext();
        var support = UserByEmail(ctx, SeedData.Support1Email);
        var admin = UserByEmail(ctx, SeedData.AdminEmail);
        var ticket = await NewTicketAsync(ctx);

        // A new ticket must be taken into work first.
        await Assert.ThrowsAsync<ConflictException>(
            () => ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, admin.Id, UserRole.Admin));

        await ctx.Service.AssignAsync(ticket.Id, support.Id);

        // Back to New and to the same state are not allowed.
        await Assert.ThrowsAsync<ConflictException>(
            () => ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.New, support.Id, UserRole.Support));
        await Assert.ThrowsAsync<ConflictException>(
            () => ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.InProgress, support.Id, UserRole.Support));
    }

    [Fact]
    public async Task GetLog_OwnerAndStaffCanRead_OtherUserIsForbidden_UnknownTicketIsNotFound()
    {
        using var ctx = CreateContext();
        var ticket = await NewTicketAsync(ctx, SeedData.User1Email);
        var owner = UserByEmail(ctx, SeedData.User1Email);
        var stranger = UserByEmail(ctx, SeedData.User2Email);

        var ownLog = await ctx.Service.GetLogAsync(ticket.Id, owner.Id, UserRole.User);

        Assert.Equal("Created", Assert.Single(ownLog).Action);
        await Assert.ThrowsAsync<ForbiddenException>(() => ctx.Service.GetLogAsync(ticket.Id, stranger.Id, UserRole.User));
        await Assert.ThrowsAsync<NotFoundException>(() => ctx.Service.GetLogAsync(9999, owner.Id, UserRole.User));
    }
}
