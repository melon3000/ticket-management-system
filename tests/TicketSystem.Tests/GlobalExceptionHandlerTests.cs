using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TicketSystem.Api.Infrastructure;

namespace TicketSystem.Tests;

public class GlobalExceptionHandlerTests
{
    private static async Task<(DefaultHttpContext Context, JsonElement Body, bool Handled)> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        context.Request.Method = "GET";
        context.Request.Path = "/api/tickets/5";
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (context, body, handled);
    }

    [Fact]
    public async Task NotFoundException_ReturnsProblemDetailsWith404AndMessage()
    {
        var (context, body, handled) = await HandleAsync(new NotFoundException("Ticket 5 was not found"));

        Assert.True(handled);
        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Ticket 5 was not found", body.GetProperty("detail").GetString());
        Assert.Equal("/api/tickets/5", body.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutInternalDetails()
    {
        var (context, body, handled) = await HandleAsync(new InvalidOperationException("secret database detail"));

        Assert.True(handled);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain("secret database detail", body.GetRawText());
        Assert.True(body.TryGetProperty("traceId", out _));
    }
}
