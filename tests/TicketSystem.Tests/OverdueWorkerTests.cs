using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TicketSystem.Worker;

namespace TicketSystem.Tests;

public class OverdueWorkerTests
{
    private static OverdueTicketProcessor CreateProcessor(Mock<ITicketApiClient> api) =>
        new(api.Object, NullLogger<OverdueTicketProcessor>.Instance);

    [Fact]
    public async Task RunOnce_EscalatesOnlyTicketsThatAreNotEscalatedYet()
    {
        var api = new Mock<ITicketApiClient>();
        api.Setup(a => a.GetOverdueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new OverdueTicket(1, "Uus", false),
            new OverdueTicket(2, "Juba tõstetud", true),
            new OverdueTicket(3, "Teine uus", false)
        });
        api.Setup(a => a.EscalateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var escalated = await CreateProcessor(api).RunOnceAsync(CancellationToken.None);

        Assert.Equal(2, escalated);
        api.Verify(a => a.EscalateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.EscalateAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.EscalateAsync(2, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnce_OneFailedTicketDoesNotStopTheOthers_AndRefusedTicketsAreNotCounted()
    {
        var api = new Mock<ITicketApiClient>();
        api.Setup(a => a.GetOverdueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new OverdueTicket(1, "Katki", false),
            new OverdueTicket(2, "Keelatud", false),
            new OverdueTicket(3, "Korras", false)
        });
        api.Setup(a => a.EscalateAsync(1, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("API viga"));
        api.Setup(a => a.EscalateAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        api.Setup(a => a.EscalateAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var escalated = await CreateProcessor(api).RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, escalated);
        api.Verify(a => a.EscalateAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunOnce_NothingOverdue_EscalatesNothing()
    {
        var api = new Mock<ITicketApiClient>();
        api.Setup(a => a.GetOverdueAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<OverdueTicket>());

        var escalated = await CreateProcessor(api).RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, escalated);
        api.Verify(a => a.EscalateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Answers HTTP requests from a function and remembers what was asked.</summary>
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string? Authorization)> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add((request.Method, request.RequestUri!.AbsolutePath, request.Headers.Authorization?.ToString()));
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static TicketApiClient CreateClient(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5119/") };
        var options = Options.Create(new ApiOptions { Email = "service@ticketsystem.local", Password = "Password123!" });
        return new TicketApiClient(http, options);
    }

    [Fact]
    public async Task Client_SignsInOnceAndSendsTheTokenWithEveryCall()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/auth/login" => JsonResponse(HttpStatusCode.OK,
                """{"token":"abc","expiresAt":"2026-11-17T12:00:00Z","user":{"id":6}}"""),
            "/api/tickets/overdue" => JsonResponse(HttpStatusCode.OK,
                """[{"id":7,"title":"Printer","status":"InProgress","isOverdue":false}]"""),
            _ => JsonResponse(HttpStatusCode.OK, "{}")
        });
        var client = CreateClient(handler);

        var overdue = await client.GetOverdueAsync(CancellationToken.None);
        var escalated = await client.EscalateAsync(7, CancellationToken.None);

        var ticket = Assert.Single(overdue);
        Assert.Equal(7, ticket.Id);
        Assert.False(ticket.IsOverdue);
        Assert.True(escalated);
        Assert.Single(handler.Calls, c => c.Path == "/api/auth/login");
        Assert.Contains(handler.Calls, c => c.Method == HttpMethod.Get && c.Path == "/api/tickets/overdue"
                                            && c.Authorization == "Bearer abc");
        Assert.Contains(handler.Calls, c => c.Method == HttpMethod.Patch && c.Path == "/api/tickets/7/escalate"
                                            && c.Authorization == "Bearer abc");
    }

    [Fact]
    public async Task Client_Conflict_MeansNotEscalated_OtherErrorsAreThrown()
    {
        var handler = new StubHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/auth/login")
            {
                return JsonResponse(HttpStatusCode.OK, """{"token":"abc","expiresAt":"2026-11-17T12:00:00Z","user":{"id":6}}""");
            }

            return path.EndsWith("/1/escalate")
                ? JsonResponse(HttpStatusCode.Conflict, "{}")
                : JsonResponse(HttpStatusCode.InternalServerError, "{}");
        });
        var client = CreateClient(handler);

        Assert.False(await client.EscalateAsync(1, CancellationToken.None));
        await Assert.ThrowsAsync<HttpRequestException>(() => client.EscalateAsync(2, CancellationToken.None));
    }

    [Theory]
    [InlineData("http://localhost:5119", "service@ticketsystem.local", "Password123!", 15, true)]
    [InlineData("not an address", "service@ticketsystem.local", "Password123!", 15, false)]
    [InlineData("http://localhost:5119", "", "Password123!", 15, false)]
    [InlineData("http://localhost:5119", "service@ticketsystem.local", "", 15, false)]
    [InlineData("http://localhost:5119", "service@ticketsystem.local", "Password123!", 0, false)]
    public void Options_AreValidOnlyWhenEverySettingIsUsable(
        string baseUrl, string email, string password, int minutes, bool expected)
    {
        var options = new ApiOptions { BaseUrl = baseUrl, Email = email, Password = password, IntervalMinutes = minutes };

        Assert.Equal(expected, options.IsValid());
    }
}
