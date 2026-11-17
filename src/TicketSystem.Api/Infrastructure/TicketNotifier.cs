using System.Text.Json;
using System.Text.Json.Serialization;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Infrastructure;

/// <summary>
/// Sends ticket messages over WebSocket as JSON: <c>{ "type": "TicketCreated", "ticket": { ... } }</c>.
/// The ticket has the same shape as in the REST API.
/// </summary>
public class TicketNotifier(WebSocketConnections connections, ILogger<TicketNotifier> logger) : ITicketNotifier
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task NotifyAsync(string eventType, TicketDto ticket)
    {
        try
        {
            var message = JsonSerializer.Serialize(new { type = eventType, ticket }, Json);
            await connections.BroadcastAsync(message);
        }
        catch (Exception ex)
        {
            // The ticket is already saved, so a broken notification must not turn the request into an error.
            logger.LogError(ex, "Could not send {EventType} for ticket {TicketId}.", eventType, ticket.Id);
        }
    }
}
