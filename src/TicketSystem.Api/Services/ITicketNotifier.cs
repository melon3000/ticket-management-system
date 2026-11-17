using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

/// <summary>Names of the real-time messages. Clients read them from the "type" field.</summary>
public static class TicketEvents
{
    /// <summary>A new ticket was created.</summary>
    public const string Created = "TicketCreated";

    /// <summary>A ticket was edited, taken into work, resolved, reopened or escalated.</summary>
    public const string Updated = "TicketUpdated";
}

/// <summary>Tells the connected support staff about ticket changes the moment they are saved.</summary>
public interface ITicketNotifier
{
    /// <summary>
    /// Sends the ticket to everyone who is listening. A failure here never fails the request that changed the
    /// ticket, because the data is already saved.
    /// </summary>
    Task NotifyAsync(string eventType, TicketDto ticket);
}
