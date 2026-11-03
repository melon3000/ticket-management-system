namespace TicketSystem.Api.Data.Entities;

/// <summary>Lifecycle state of a ticket. Stored in the database as text.</summary>
public enum TicketStatus
{
    /// <summary>Created, nobody has taken it yet.</summary>
    New,

    /// <summary>A support staff member is working on it.</summary>
    InProgress,

    /// <summary>The problem is solved.</summary>
    Resolved
}
