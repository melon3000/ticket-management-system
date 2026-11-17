namespace TicketSystem.Worker;

/// <summary>The two API calls of the Worker. The Worker never touches the database.</summary>
public interface ITicketApiClient
{
    /// <summary>GET /api/tickets/overdue: the unresolved tickets that have missed their deadline.</summary>
    Task<IReadOnlyList<OverdueTicket>> GetOverdueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// PATCH /api/tickets/{id}/escalate. Returns true when the ticket was escalated and false when the API
    /// refused with 409, for example because somebody resolved the ticket in the meantime.
    /// Any other error is thrown as HttpRequestException.
    /// </summary>
    Task<bool> EscalateAsync(int ticketId, CancellationToken cancellationToken);
}
