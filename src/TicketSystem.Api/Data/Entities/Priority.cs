namespace TicketSystem.Api.Data.Entities;

/// <summary>Urgency of a ticket. The priority decides how long the deadline is.</summary>
public class Priority
{
    public int Id { get; set; }

    /// <summary>Priority name. Unique.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Order of importance: 1 is the lowest. Unique.</summary>
    public int Level { get; set; }

    /// <summary>Hours between creation and the deadline.</summary>
    public int DeadlineHours { get; set; }

    public List<Ticket> Tickets { get; set; } = new();
}
