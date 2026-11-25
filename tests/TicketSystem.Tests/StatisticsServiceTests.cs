using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class StatisticsServiceTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);

    private sealed record Context(SqliteConnection Connection, AppDbContext Db, StatisticsService Service)
        : IDisposable
    {
        public void Dispose() => Connection.Dispose();
    }

    /// <summary>A database with the seeded users, categories and priorities but no tickets.</summary>
    private static Context CreateContext()
    {
        var (connection, db) = TestDatabase.CreateSeeded(withTickets: false);

        var service = new StatisticsService(new TicketRepository(db), new CategoryRepository(db), new UserRepository(db));
        return new Context(connection, db, service);
    }

    private static int UserId(Context ctx, string email) => ctx.Db.Users.Single(u => u.Email == email).Id;

    /// <summary>Stores a ticket that took <paramref name="hours"/> hours to resolve, or an unresolved one when null.</summary>
    private static void AddTicket(Context ctx, int categoryId, string? supportEmail, double? hours)
    {
        ctx.Db.Tickets.Add(new Ticket
        {
            Title = "Tiket statistika jaoks",
            Description = "Kirjeldus on piisavalt pikk.",
            CategoryId = categoryId,
            PriorityId = ctx.Db.Priorities.First().Id,
            CreatedById = UserId(ctx, SeedData.User1Email),
            AssigneeId = supportEmail is null ? null : UserId(ctx, supportEmail),
            Status = hours is null ? TicketStatus.InProgress : TicketStatus.Resolved,
            CreatedAt = Start,
            Deadline = Start.AddHours(24),
            ResolvedAt = hours is null ? null : Start.AddHours(hours.Value)
        });
        ctx.Db.SaveChanges();
    }

    [Fact]
    public async Task GetResolutionTimes_WithoutResolvedTickets_ListsEveryoneWithNoAverage()
    {
        using var ctx = CreateContext();

        var result = await ctx.Service.GetResolutionTimesAsync();

        Assert.Equal(0, result.ResolvedCount);
        Assert.Null(result.AverageHours);
        Assert.Equal(ctx.Db.Categories.Count(), result.ByCategory.Count);
        Assert.Equal(2, result.BySupport.Count);
        Assert.All(result.ByCategory, c => Assert.Null(c.AverageHours));
        Assert.All(result.BySupport, s => Assert.Equal(0, s.ResolvedCount));
    }

    [Fact]
    public async Task GetResolutionTimes_AveragesPerCategoryAndPerSupportPerson()
    {
        using var ctx = CreateContext();
        var categories = ctx.Db.Categories.OrderBy(c => c.Id).Take(2).ToList();
        AddTicket(ctx, categories[0].Id, SeedData.Support1Email, 2);
        AddTicket(ctx, categories[0].Id, SeedData.Support1Email, 4);
        AddTicket(ctx, categories[1].Id, SeedData.Support2Email, 10);

        var result = await ctx.Service.GetResolutionTimesAsync();

        Assert.Equal(3, result.ResolvedCount);
        Assert.Equal(5.33, result.AverageHours);

        var first = result.ByCategory.Single(c => c.Id == categories[0].Id);
        Assert.Equal(2, first.ResolvedCount);
        Assert.Equal(3, first.AverageHours);
        var second = result.ByCategory.Single(c => c.Id == categories[1].Id);
        Assert.Equal(1, second.ResolvedCount);
        Assert.Equal(10, second.AverageHours);

        var mari = result.BySupport.Single(s => s.Id == UserId(ctx, SeedData.Support1Email));
        Assert.Equal("Mari Maasikas", mari.Name);
        Assert.Equal(2, mari.ResolvedCount);
        Assert.Equal(3, mari.AverageHours);
        var jaan = result.BySupport.Single(s => s.Id == UserId(ctx, SeedData.Support2Email));
        Assert.Equal(10, jaan.AverageHours);
    }

    [Fact]
    public async Task GetResolutionTimes_IgnoresUnresolvedTickets()
    {
        using var ctx = CreateContext();
        var categoryId = ctx.Db.Categories.OrderBy(c => c.Id).First().Id;
        AddTicket(ctx, categoryId, SeedData.Support1Email, 6);
        AddTicket(ctx, categoryId, SeedData.Support1Email, null);
        AddTicket(ctx, categoryId, null, null);

        var result = await ctx.Service.GetResolutionTimesAsync();

        Assert.Equal(1, result.ResolvedCount);
        Assert.Equal(6, result.AverageHours);
        Assert.Equal(6, result.ByCategory.Single(c => c.Id == categoryId).AverageHours);
    }

    [Fact]
    public async Task GetResolutionTimes_CategoryWithoutResolvedTickets_HasNoAverage()
    {
        using var ctx = CreateContext();
        var categories = ctx.Db.Categories.OrderBy(c => c.Id).Take(2).ToList();
        AddTicket(ctx, categories[0].Id, SeedData.Support1Email, 1.5);

        var result = await ctx.Service.GetResolutionTimesAsync();

        var empty = result.ByCategory.Single(c => c.Id == categories[1].Id);
        Assert.Equal(0, empty.ResolvedCount);
        Assert.Null(empty.AverageHours);
        Assert.Equal(1.5, result.ByCategory.Single(c => c.Id == categories[0].Id).AverageHours);
    }
}
