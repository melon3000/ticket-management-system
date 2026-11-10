using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class TicketListTests
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

    /// <summary>A database with the seeded users, categories and priorities but no tickets.</summary>
    private static Context CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        SeedData.Initialize(db);
        db.Tickets.RemoveRange(db.Tickets.ToList());
        db.SaveChanges();

        var service = new TicketService(
            new TicketRepository(db), new CategoryRepository(db), new PriorityRepository(db),
            new UserRepository(db), new TicketLogRepository(db), new UnitOfWork(db),
            new FixedClock(), NullLogger<TicketService>.Instance);
        return new Context(connection, db, service);
    }

    private static async Task<TicketDto> CreateAsync(
        Context ctx, string authorEmail, string title, string priority = "Keskmine",
        string description = "Kirjeldus on piisavalt pikk.")
    {
        var author = ctx.Db.Users.Single(u => u.Email == authorEmail);
        return await ctx.Service.CreateAsync(new CreateTicketRequest
        {
            Title = title,
            Description = description,
            CategoryId = ctx.Db.Categories.First().Id,
            PriorityId = ctx.Db.Priorities.Single(p => p.Name == priority).Id
        }, author.Id);
    }

    private static Task<PagedResult<TicketDto>> ListAsync(Context ctx, string email, TicketQuery query)
    {
        var user = ctx.Db.Users.Single(u => u.Email == email);
        return ctx.Service.ListAsync(query, user.Id, user.Role);
    }

    private static List<string> Titles(PagedResult<TicketDto> page) => page.Items.Select(t => t.Title).ToList();

    [Fact]
    public async Task List_OrdinaryUserSeesOnlyOwnTickets_StaffSeeAll()
    {
        using var ctx = CreateContext();
        await CreateAsync(ctx, SeedData.User1Email, "Katile esimene");
        await CreateAsync(ctx, SeedData.User1Email, "Katile teine");
        await CreateAsync(ctx, SeedData.User2Email, "Toomase oma");

        var own = await ListAsync(ctx, SeedData.User1Email, new TicketQuery());
        var support = await ListAsync(ctx, SeedData.Support1Email, new TicketQuery());
        var admin = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery());

        Assert.Equal(2, own.TotalCount);
        Assert.DoesNotContain("Toomase oma", Titles(own));
        Assert.Equal(3, support.TotalCount);
        Assert.Equal(3, admin.TotalCount);
    }

    [Fact]
    public async Task List_FiltersByStatusPriorityAndAssignee()
    {
        using var ctx = CreateContext();
        await CreateAsync(ctx, SeedData.User1Email, "Madal tiket", "Madal");
        var high1 = await CreateAsync(ctx, SeedData.User1Email, "Kõrge töös", "Kõrge");
        await CreateAsync(ctx, SeedData.User2Email, "Kõrge uus", "Kõrge");

        var support = ctx.Db.Users.Single(u => u.Email == SeedData.Support1Email);
        var inProgress = ctx.Db.Tickets.Single(t => t.Id == high1.Id);
        inProgress.Status = TicketStatus.InProgress;
        inProgress.AssigneeId = support.Id;
        ctx.Db.SaveChanges();
        var highId = ctx.Db.Priorities.Single(p => p.Name == "Kõrge").Id;

        var byStatus = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Status = TicketStatus.InProgress });
        var byPriority = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Priority = highId });
        var byAssignee = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Assignee = support.Id });
        var combined = await ListAsync(ctx, SeedData.AdminEmail,
            new TicketQuery { Status = TicketStatus.New, Priority = highId });

        Assert.Equal(new[] { "Kõrge töös" }, Titles(byStatus));
        Assert.Equal(2, byPriority.TotalCount);
        Assert.Equal(new[] { "Kõrge töös" }, Titles(byAssignee));
        Assert.Equal(new[] { "Kõrge uus" }, Titles(combined));
    }

    [Fact]
    public async Task List_SearchesTitleAndDescriptionIgnoringCase_AndTreatsPercentAsText()
    {
        using var ctx = CreateContext();
        await CreateAsync(ctx, SeedData.User1Email, "Printer ei tööta");
        await CreateAsync(ctx, SeedData.User1Email, "Wifi katkeb", description: "Aula printer on kaugel ruuterist.");
        await CreateAsync(ctx, SeedData.User1Email, "Konto lukus", description: "Parooli lähtestamine ebaõnnestub.");
        await CreateAsync(ctx, SeedData.User1Email, "Kiirus alla 50% normist");

        var printer = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Search = "PRINTER", SortBy = TicketSortField.CreatedAt, Descending = false });
        var percent = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Search = "%" });
        var none = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Search = "olematu" });

        Assert.Equal(new[] { "Printer ei tööta", "Wifi katkeb" }, Titles(printer));
        Assert.Equal(new[] { "Kiirus alla 50% normist" }, Titles(percent));
        Assert.Empty(none.Items);
        Assert.Equal(0, none.TotalPages);
    }

    [Fact]
    public async Task List_SortsByPriorityAndDeadline()
    {
        using var ctx = CreateContext();
        await CreateAsync(ctx, SeedData.User1Email, "Madal", "Madal");
        await CreateAsync(ctx, SeedData.User1Email, "Kriitiline", "Kriitiline");
        await CreateAsync(ctx, SeedData.User1Email, "Keskmine", "Keskmine");

        var urgentFirst = await ListAsync(ctx, SeedData.AdminEmail,
            new TicketQuery { SortBy = TicketSortField.Priority, Descending = true });
        var lowFirst = await ListAsync(ctx, SeedData.AdminEmail,
            new TicketQuery { SortBy = TicketSortField.Priority, Descending = false });
        var soonestDeadline = await ListAsync(ctx, SeedData.AdminEmail,
            new TicketQuery { SortBy = TicketSortField.Deadline, Descending = false });

        Assert.Equal(new[] { "Kriitiline", "Keskmine", "Madal" }, Titles(urgentFirst));
        Assert.Equal(new[] { "Madal", "Keskmine", "Kriitiline" }, Titles(lowFirst));
        Assert.Equal(new[] { "Kriitiline", "Keskmine", "Madal" }, Titles(soonestDeadline));
    }

    [Fact]
    public async Task List_ReturnsTimesMarkedAsUtc()
    {
        using var ctx = CreateContext();
        var created = await CreateAsync(ctx, SeedData.User1Email, "Ajatsoon");
        ctx.Db.Tickets.Single(t => t.Id == created.Id).ResolvedAt = FixedNow.AddHours(1);
        ctx.Db.SaveChanges();

        var page = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery());

        var ticket = Assert.Single(page.Items);
        Assert.Equal(DateTimeKind.Utc, ticket.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, ticket.Deadline.Kind);
        Assert.Equal(DateTimeKind.Utc, ticket.ResolvedAt!.Value.Kind);
        Assert.Equal(FixedNow, ticket.CreatedAt);
    }

    [Fact]
    public async Task List_ReturnsRequestedPageWithTotals()
    {
        using var ctx = CreateContext();
        for (var i = 1; i <= 5; i++) await CreateAsync(ctx, SeedData.User1Email, $"Tiket {i}");

        var first = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Page = 1, PageSize = 2 });
        var last = await ListAsync(ctx, SeedData.AdminEmail, new TicketQuery { Page = 3, PageSize = 2 });
        var oldestFirst = await ListAsync(ctx, SeedData.AdminEmail,
            new TicketQuery { Page = 1, PageSize = 2, Descending = false });

        Assert.Equal(new[] { "Tiket 5", "Tiket 4" }, Titles(first));
        Assert.Equal(5, first.TotalCount);
        Assert.Equal(3, first.TotalPages);
        Assert.Equal(new[] { "Tiket 1" }, Titles(last));
        Assert.Equal(new[] { "Tiket 1", "Tiket 2" }, Titles(oldestFirst));
    }
}
