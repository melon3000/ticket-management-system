using TicketSystem.Api.Auth;

namespace TicketSystem.Api.Infrastructure;

/// <summary>The /ws/tickets channel: real-time ticket messages for support staff.</summary>
public static class TicketWebSocketExtensions
{
    /// <summary>Address of the channel.</summary>
    public const string Path = "/ws/tickets";

    /// <summary>
    /// Opens the channel for signed-in support staff and administrators. Browsers cannot set the Authorization header
    /// of a WebSocket, so the JWT is also accepted in the query string: <c>/ws/tickets?access_token=...</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapTicketWebSocket(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, async (HttpContext context, WebSocketConnections connections) =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Bad Request",
                        detail: "This address only accepts WebSocket connections.");
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await connections.RunAsync(socket, context.RequestAborted);
                return Results.Empty;
            })
            .RequireAuthorization(Policies.SupportOrAdmin)
            .ExcludeFromDescription();
        return app;
    }
}
