using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class UserAdminServiceTests
{
    private sealed record Context(SqliteConnection Connection, AppDbContext Db, UserAdminService Service) : IDisposable
    {
        public void Dispose() => Connection.Dispose();
    }

    private static Context CreateContext()
    {
        var (connection, db) = TestDatabase.CreateSeeded();
        var service = new UserAdminService(
            new UserRepository(db), new TicketRepository(db), new UnitOfWork(db),
            NullLogger<UserAdminService>.Instance);
        return new Context(connection, db, service);
    }

    private static int IdOf(Context ctx, string email) => ctx.Db.Users.Single(u => u.Email == email).Id;

    /// <summary>The id of a support person who has an unresolved ticket in work in the starting data.</summary>
    private static int SupportWithOpenTicket(Context ctx) =>
        ctx.Db.Tickets
            .Where(t => t.AssigneeId != null && t.Status != TicketStatus.Resolved)
            .Select(t => t.AssigneeId!.Value)
            .First();

    [Fact]
    public async Task GetAll_ReturnsEveryUserWithEmailAndRoleSortedByName()
    {
        using var ctx = CreateContext();

        var users = await ctx.Service.GetAllAsync();

        Assert.Equal(6, users.Count);
        Assert.Equal(users.Select(u => u.DisplayName).OrderBy(n => n, StringComparer.Ordinal).ToList(),
            users.Select(u => u.DisplayName).ToList());
        var kati = users.Single(u => u.Email == SeedData.User1Email);
        Assert.Equal(UserRole.User, kati.Role);
    }

    [Fact]
    public async Task ChangeRole_GivesUserTheSupportRoleAndStoresIt()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);
        var katiId = IdOf(ctx, SeedData.User1Email);

        var result = await ctx.Service.ChangeRoleAsync(adminId, katiId, UserRole.Support);

        Assert.Equal(UserRole.Support, result.Role);
        ctx.Db.ChangeTracker.Clear();
        Assert.Equal(UserRole.Support, ctx.Db.Users.Single(u => u.Id == katiId).Role);
    }

    [Fact]
    public async Task ChangeRole_SameRole_ChangesNothing()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);
        var katiId = IdOf(ctx, SeedData.User1Email);

        var result = await ctx.Service.ChangeRoleAsync(adminId, katiId, UserRole.User);

        Assert.Equal(UserRole.User, result.Role);
    }

    [Fact]
    public async Task ChangeRole_OwnRole_IsRefused()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);

        await Assert.ThrowsAsync<ConflictException>(() =>
            ctx.Service.ChangeRoleAsync(adminId, adminId, UserRole.User));

        Assert.Equal(UserRole.Admin, ctx.Db.Users.Single(u => u.Id == adminId).Role);
    }

    [Fact]
    public async Task ChangeRole_ServiceAccount_IsRefused()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);
        var serviceId = IdOf(ctx, SeedData.ServiceEmail);

        await Assert.ThrowsAsync<ConflictException>(() =>
            ctx.Service.ChangeRoleAsync(adminId, serviceId, UserRole.User));
    }

    [Fact]
    public async Task ChangeRole_ServiceRoleCannotBeGivenToAPerson()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);
        var katiId = IdOf(ctx, SeedData.User1Email);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            ctx.Service.ChangeRoleAsync(adminId, katiId, UserRole.Service));
    }

    [Fact]
    public async Task ChangeRole_UnknownUser_IsNotFound()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            ctx.Service.ChangeRoleAsync(adminId, 9999, UserRole.Support));
    }

    [Fact]
    public async Task ChangeRole_SupportWithUnresolvedTicket_IsRefusedAndRoleStays()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);
        var supportId = SupportWithOpenTicket(ctx);

        await Assert.ThrowsAsync<ConflictException>(() =>
            ctx.Service.ChangeRoleAsync(adminId, supportId, UserRole.User));

        ctx.Db.ChangeTracker.Clear();
        Assert.Equal(UserRole.Support, ctx.Db.Users.Single(u => u.Id == supportId).Role);
    }

    [Fact]
    public async Task ChangeRole_SupportWhoResolvedEverything_BecomesUser()
    {
        using var ctx = CreateContext();
        var adminId = IdOf(ctx, SeedData.AdminEmail);
        var supportId = SupportWithOpenTicket(ctx);
        foreach (var ticket in ctx.Db.Tickets.Where(t => t.AssigneeId == supportId && t.Status != TicketStatus.Resolved).ToList())
        {
            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = ticket.CreatedAt.AddHours(1);
        }
        ctx.Db.SaveChanges();

        var result = await ctx.Service.ChangeRoleAsync(adminId, supportId, UserRole.User);

        Assert.Equal(UserRole.User, result.Role);
    }
}
