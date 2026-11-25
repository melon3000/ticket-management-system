using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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

public class TicketNotificationTests
{
    private static readonly DateTime Start = new(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);

    private sealed class TestClock : TimeProvider
    {
        public DateTime Now { get; set; } = Start;
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private sealed record Context(
        SqliteConnection Connection, AppDbContext Db, TicketService Service, Mock<ITicketNotifier> Notifier, TestClock Clock)
        : IDisposable
    {
        public void Dispose() => Connection.Dispose();
    }

    private static Context CreateContext()
    {
        var (connection, db) = TestDatabase.CreateSeeded();

        var notifier = new Mock<ITicketNotifier>();
        notifier.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<TicketDto>())).Returns(Task.CompletedTask);
        var clock = new TestClock();
        var service = new TicketService(
            new TicketRepository(db), new CategoryRepository(db), new PriorityRepository(db),
            new UserRepository(db), new TicketLogRepository(db), new UnitOfWork(db), notifier.Object,
            clock, NullLogger<TicketService>.Instance);
        return new Context(connection, db, service, notifier, clock);
    }

    private static User UserByEmail(Context ctx, string email) => ctx.Db.Users.Single(u => u.Email == email);

    private static CreateTicketRequest Request(Context ctx, string priority = "Keskmine") => new()
    {
        Title = "Projektor ei käivitu",
        Description = "Klassiruumi projektor ei lülitu sisse.",
        CategoryId = ctx.Db.Categories.First().Id,
        PriorityId = ctx.Db.Priorities.Single(p => p.Name == priority).Id
    };

    [Fact]
    public async Task CreateAssignAndResolve_SendTheMatchingMessagesAfterSaving()
    {
        using var ctx = CreateContext();
        var author = UserByEmail(ctx, SeedData.User1Email);
        var support = UserByEmail(ctx, SeedData.Support1Email);

        var ticket = await ctx.Service.CreateAsync(Request(ctx), author.Id);
        await ctx.Service.AssignAsync(ticket.Id, support.Id);
        await ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, support.Id, UserRole.Support);

        ctx.Notifier.Verify(n => n.NotifyAsync(TicketEvents.Created,
            It.Is<TicketDto>(t => t.Id == ticket.Id && t.Status == TicketStatus.New)), Times.Once);
        ctx.Notifier.Verify(n => n.NotifyAsync(TicketEvents.Updated,
            It.Is<TicketDto>(t => t.Id == ticket.Id && t.Status == TicketStatus.InProgress)), Times.Once);
        ctx.Notifier.Verify(n => n.NotifyAsync(TicketEvents.Updated,
            It.Is<TicketDto>(t => t.Id == ticket.Id && t.Status == TicketStatus.Resolved)), Times.Once);
    }

    [Fact]
    public async Task EditingATicket_SendsTicketUpdated()
    {
        using var ctx = CreateContext();
        var author = UserByEmail(ctx, SeedData.User1Email);
        var ticket = await ctx.Service.CreateAsync(Request(ctx), author.Id);
        var edit = new UpdateTicketRequest
        {
            Title = "Projektor on katki",
            Description = "Klassiruumi projektor ei lülitu sisse.",
            CategoryId = ticket.Category.Id,
            PriorityId = ticket.Priority.Id
        };

        await ctx.Service.UpdateAsync(ticket.Id, edit, author.Id, UserRole.User);

        ctx.Notifier.Verify(n => n.NotifyAsync(TicketEvents.Updated,
            It.Is<TicketDto>(t => t.Id == ticket.Id && t.Title == "Projektor on katki")), Times.Once);
    }

    [Fact]
    public async Task RefusedChanges_SendNothing()
    {
        using var ctx = CreateContext();
        var author = UserByEmail(ctx, SeedData.User1Email);
        var support = UserByEmail(ctx, SeedData.Support1Email);
        var otherSupport = UserByEmail(ctx, SeedData.Support2Email);
        var ticket = await ctx.Service.CreateAsync(Request(ctx), author.Id);
        await ctx.Service.AssignAsync(ticket.Id, support.Id);
        ctx.Notifier.Invocations.Clear();

        await Assert.ThrowsAsync<ConflictException>(() => ctx.Service.AssignAsync(ticket.Id, otherSupport.Id));
        await Assert.ThrowsAsync<ForbiddenException>(
            () => ctx.Service.ChangeStatusAsync(ticket.Id, TicketStatus.Resolved, otherSupport.Id, UserRole.Support));

        ctx.Notifier.Verify(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<TicketDto>()), Times.Never);
    }

    [Fact]
    public async Task Escalation_SendsTicketUpdatedWithTheOverdueFlag()
    {
        using var ctx = CreateContext();
        var author = UserByEmail(ctx, SeedData.User1Email);
        var service = UserByEmail(ctx, SeedData.ServiceEmail);
        var ticket = await ctx.Service.CreateAsync(Request(ctx, "Kriitiline"), author.Id);
        ctx.Clock.Now = Start.AddHours(5);

        await ctx.Service.EscalateAsync(ticket.Id, service.Id);

        ctx.Notifier.Verify(n => n.NotifyAsync(TicketEvents.Updated,
            It.Is<TicketDto>(t => t.Id == ticket.Id && t.IsOverdue)), Times.Once);
    }

    /// <summary>A WebSocket that records the messages it is sent and stays open until the test closes it.</summary>
    private sealed class FakeSocket : WebSocket
    {
        private readonly TaskCompletionSource<WebSocketReceiveResult> _receive = new();
        private WebSocketState _state = WebSocketState.Open;

        public List<string> Sent { get; } = new();
        public bool FailOnSend { get; init; }

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override void Abort() => _state = WebSocketState.Aborted;

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            _receive.Task;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            if (FailOnSend)
            {
                throw new WebSocketException("The connection is broken.");
            }

            Sent.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
            return Task.CompletedTask;
        }

        /// <summary>The client sends a close frame.</summary>
        public void ClientCloses()
        {
            _state = WebSocketState.CloseReceived;
            _receive.SetResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "Bye"));
        }
    }

    [Fact]
    public async Task Broadcast_ReachesEveryClient_AndAClientThatLeavesIsRemoved()
    {
        var connections = new WebSocketConnections(NullLogger<WebSocketConnections>.Instance);
        var first = new FakeSocket();
        var second = new FakeSocket();
        var firstRun = connections.RunAsync(first, CancellationToken.None);
        _ = connections.RunAsync(second, CancellationToken.None);

        await connections.BroadcastAsync("tere");

        Assert.Equal(2, connections.Count);
        Assert.Equal(new[] { "tere" }, first.Sent.ToArray());
        Assert.Equal(new[] { "tere" }, second.Sent.ToArray());

        first.ClientCloses();
        await firstRun;
        await connections.BroadcastAsync("veel");

        Assert.Equal(1, connections.Count);
        Assert.Equal(new[] { "tere" }, first.Sent.ToArray());
        Assert.Equal(new[] { "tere", "veel" }, second.Sent.ToArray());
    }

    [Fact]
    public async Task Broadcast_AClientThatFailsIsDropped_TheOthersStillGetTheMessage()
    {
        var connections = new WebSocketConnections(NullLogger<WebSocketConnections>.Instance);
        var healthy = new FakeSocket();
        var broken = new FakeSocket { FailOnSend = true };
        _ = connections.RunAsync(healthy, CancellationToken.None);
        _ = connections.RunAsync(broken, CancellationToken.None);

        await connections.BroadcastAsync("tere");

        Assert.Equal(new[] { "tere" }, healthy.Sent.ToArray());
        Assert.Equal(1, connections.Count);
        Assert.Equal(WebSocketState.Aborted, broken.State);
    }

    [Fact]
    public async Task Notifier_SendsJsonWithTheTypeAndTheTicketInTheSameShapeAsTheRestApi()
    {
        var connections = new WebSocketConnections(NullLogger<WebSocketConnections>.Instance);
        var socket = new FakeSocket();
        _ = connections.RunAsync(socket, CancellationToken.None);
        var notifier = new TicketNotifier(connections, NullLogger<TicketNotifier>.Instance);
        var ticket = new TicketDto(
            5, "Printer ei tööta", "Kirjeldus", TicketStatus.New, new CategoryDto(1, "Riistvara"),
            new PriorityDto(2, "Keskmine", 2, 24), new UserSummaryDto(1, "Kati Kask"), null,
            Start, Start.AddHours(24), null, false);

        await notifier.NotifyAsync(TicketEvents.Created, ticket);

        using var document = JsonDocument.Parse(Assert.Single(socket.Sent));
        var root = document.RootElement;
        var sent = root.GetProperty("ticket");
        Assert.Equal("TicketCreated", root.GetProperty("type").GetString());
        Assert.Equal(5, sent.GetProperty("id").GetInt32());
        Assert.Equal("New", sent.GetProperty("status").GetString());
        Assert.Equal("Keskmine", sent.GetProperty("priority").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, sent.GetProperty("assignee").ValueKind);
        Assert.False(sent.GetProperty("isOverdue").GetBoolean());
    }
}
