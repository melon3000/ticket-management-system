namespace TicketSystem.Api.Data.Entities;

/// <summary>One entry of the change log: who did what and when.</summary>
public class TicketLog
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>User who made the change (the Service account for escalations).</summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>What happened. One of <see cref="TicketLogActions"/>.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Value before the change. Empty when there was none.</summary>
    public string? OldValue { get; set; }

    /// <summary>Value after the change.</summary>
    public string? NewValue { get; set; }

    /// <summary>Time of the change in UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
