using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class CommentServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(FixedNow);
    }

    private sealed record Context(
        SqliteConnection Connection, AppDbContext Db, TicketService Tickets, CommentService Comments) : IDisposable
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

        var tickets = new TicketRepository(db);
        var users = new UserRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var clock = new FixedClock();
        var ticketService = new TicketService(
            tickets, new CategoryRepository(db), new PriorityRepository(db), users,
            new TicketLogRepository(db), unitOfWork, new NullTicketNotifier(), clock, NullLogger<TicketService>.Instance);
        var commentService = new CommentService(
            tickets, new CommentRepository(db), users, unitOfWork, clock, NullLogger<CommentService>.Instance);
        return new Context(connection, db, ticketService, commentService);
    }

    private static User UserByEmail(Context ctx, string email) => ctx.Db.Users.Single(u => u.Email == email);

    private static async Task<TicketDto> NewTicketAsync(Context ctx, string authorEmail) =>
        await ctx.Tickets.CreateAsync(new CreateTicketRequest
        {
            Title = "Võrk ei tööta",
            Description = "Ruumis 214 puudub internetiühendus.",
            CategoryId = ctx.Db.Categories.First().Id,
            PriorityId = ctx.Db.Priorities.First().Id
        }, UserByEmail(ctx, authorEmail).Id);

    private static CreateCommentRequest Text(string text) => new() { Text = text };

    [Fact]
    public async Task Add_AuthorAndSupportPerson_CommentsComeBackInOrderWithAuthors()
    {
        using var ctx = CreateContext();
        var author = UserByEmail(ctx, SeedData.User1Email);
        var support = UserByEmail(ctx, SeedData.Support1Email);
        var ticket = await NewTicketAsync(ctx, SeedData.User1Email);

        await ctx.Comments.AddAsync(ticket.Id, Text("Palun vaadake üle."), author.Id, UserRole.User);
        await ctx.Comments.AddAsync(ticket.Id, Text("Vaatan täna."), support.Id, UserRole.Support);
        var list = await ctx.Comments.GetAsync(ticket.Id, author.Id, UserRole.User);

        Assert.Equal(new[] { "Palun vaadake üle.", "Vaatan täna." }, list.Select(c => c.Text).ToArray());
        Assert.Equal(new[] { author.Id, support.Id }, list.Select(c => c.Author.Id).ToArray());
        Assert.Equal(FixedNow, list[0].CreatedAt);
    }

    [Fact]
    public async Task Add_OtherOrdinaryUserIsForbidden_AdministratorIsAllowed_UnknownTicketIsNotFound()
    {
        using var ctx = CreateContext();
        var ticket = await NewTicketAsync(ctx, SeedData.User1Email);
        var stranger = UserByEmail(ctx, SeedData.User2Email);
        var admin = UserByEmail(ctx, SeedData.AdminEmail);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => ctx.Comments.AddAsync(ticket.Id, Text("Mina ka."), stranger.Id, UserRole.User));
        var added = await ctx.Comments.AddAsync(ticket.Id, Text("Administraator kirjutab."), admin.Id, UserRole.Admin);
        await Assert.ThrowsAsync<NotFoundException>(
            () => ctx.Comments.AddAsync(9999, Text("Tere."), admin.Id, UserRole.Admin));

        Assert.Equal(admin.Id, added.Author.Id);
        Assert.Single(await ctx.Comments.GetAsync(ticket.Id, admin.Id, UserRole.Admin));
    }

    [Fact]
    public async Task Get_OtherOrdinaryUserIsForbidden_SupportPersonIsAllowed()
    {
        using var ctx = CreateContext();
        var ticket = await NewTicketAsync(ctx, SeedData.User1Email);
        var stranger = UserByEmail(ctx, SeedData.User2Email);
        var support = UserByEmail(ctx, SeedData.Support2Email);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => ctx.Comments.GetAsync(ticket.Id, stranger.Id, UserRole.User));
        Assert.Empty(await ctx.Comments.GetAsync(ticket.Id, support.Id, UserRole.Support));
    }

    [Fact]
    public async Task Add_ForbiddenUser_DoesNotStoreAnything()
    {
        var tickets = new Mock<ITicketRepository>();
        var comments = new Mock<ICommentRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        tickets.Setup(t => t.GetByIdAsync(5)).ReturnsAsync(new Ticket { Id = 5, CreatedById = 1 });
        var service = new CommentService(
            tickets.Object, comments.Object, users.Object, unitOfWork.Object,
            TimeProvider.System, NullLogger<CommentService>.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.AddAsync(5, Text("Tere."), 2, UserRole.User));

        comments.Verify(c => c.AddAsync(It.IsAny<Comment>()), Times.Never);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Add_TrimsTheTextAndStoresOneComment()
    {
        var tickets = new Mock<ITicketRepository>();
        var comments = new Mock<ICommentRepository>();
        var users = new Mock<IUserRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        tickets.Setup(t => t.GetByIdAsync(5)).ReturnsAsync(new Ticket { Id = 5, CreatedById = 1 });
        users.Setup(u => u.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, DisplayName = "Kati Kask" });
        unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        Comment? stored = null;
        comments.Setup(c => c.AddAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => stored = c)
            .Returns(Task.CompletedTask);
        var service = new CommentService(
            tickets.Object, comments.Object, users.Object, unitOfWork.Object,
            TimeProvider.System, NullLogger<CommentService>.Instance);

        var result = await service.AddAsync(5, Text("   Tere!   "), 1, UserRole.User);

        Assert.Equal("Tere!", stored!.Text);
        Assert.Equal(5, stored.TicketId);
        Assert.Equal("Tere!", result.Text);
        unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
