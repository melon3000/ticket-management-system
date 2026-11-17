namespace TicketSystem.Worker;

/// <summary>The part of the login response that the Worker needs.</summary>
public record LoginResult(string Token, DateTime ExpiresAt);

/// <summary>The part of a ticket that the Worker needs.</summary>
/// <param name="Id">Ticket id.</param>
/// <param name="Title">Short title, used in the log.</param>
/// <param name="IsOverdue">True when the ticket was already escalated.</param>
public record OverdueTicket(int Id, string Title, bool IsOverdue);
