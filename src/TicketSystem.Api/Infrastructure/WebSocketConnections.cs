using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace TicketSystem.Api.Infrastructure;

/// <summary>Keeps the open WebSocket connections of the signed-in support staff and sends messages to all of them.</summary>
public class WebSocketConnections(ILogger<WebSocketConnections> logger)
{
    private sealed class Connection(WebSocket socket)
    {
        public WebSocket Socket { get; } = socket;

        /// <summary>A socket can only send one message at a time, so sends to the same client wait for each other.</summary>
        public SemaphoreSlim SendLock { get; } = new(1, 1);
    }

    private readonly ConcurrentDictionary<Guid, Connection> _connections = new();

    /// <summary>Number of clients that are connected right now.</summary>
    public int Count => _connections.Count;

    /// <summary>
    /// Registers the connection and keeps it open until the client closes it or the server stops.
    /// The channel only sends, so anything the client writes is ignored.
    /// </summary>
    public async Task RunAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        _connections[id] = new Connection(socket);
        logger.LogInformation("WebSocket client {ClientId} connected, {Count} connected.", id, Count);

        try
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
            {
                var received = await socket.ReceiveAsync(buffer.AsMemory(), cancellationToken);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }

            if (socket.State is WebSocketState.CloseReceived or WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
        catch (WebSocketException ex)
        {
            logger.LogDebug(ex, "WebSocket client {ClientId} ended without a closing handshake.", id);
        }
        finally
        {
            _connections.TryRemove(id, out _);
            logger.LogInformation("WebSocket client {ClientId} disconnected, {Count} connected.", id, Count);
        }
    }

    /// <summary>Sends a text message to every connected client. A client that fails is dropped, the others still get it.</summary>
    public async Task BroadcastAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var sends = _connections.Select(pair => SendAsync(pair.Key, pair.Value, bytes)).ToList();
        await Task.WhenAll(sends);
    }

    private async Task SendAsync(Guid id, Connection connection, byte[] bytes)
    {
        if (connection.Socket.State != WebSocketState.Open)
        {
            _connections.TryRemove(id, out _);
            return;
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await connection.SendLock.WaitAsync(timeout.Token);
            try
            {
                await connection.Socket.SendAsync(
                    new ReadOnlyMemory<byte>(bytes), WebSocketMessageType.Text, true, timeout.Token);
            }
            finally
            {
                connection.SendLock.Release();
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or ObjectDisposedException)
        {
            logger.LogWarning(ex, "Sending to WebSocket client {ClientId} failed, the client is dropped.", id);
            _connections.TryRemove(id, out _);
            connection.Socket.Abort();
        }
    }
}
