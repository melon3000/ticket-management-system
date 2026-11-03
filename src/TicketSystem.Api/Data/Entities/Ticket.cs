namespace TicketSystem.Api.Data.Entities;

/// <summary>A reported problem.</summary>
public class Ticket
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int PriorityId { get; set; }
    public Priority Priority { get; set; } = null!;

    /// <summary>User who reported the problem.</summary>
    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    /// <summary>Support staff member working on the ticket. Empty until someone takes it.</summary>
    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    /// <summary>Creation time in UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Time in UTC by which the ticket should be resolved.</summary>
    public DateTime Deadline { get; set; }

    /// <summary>Time in UTC when the ticket was resolved. Empty while unresolved.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>True when the Worker has found the ticket late and escalated it.</summary>
    public bool IsOverdue { get; set; }

    public List<Comment> Comments { get; set; } = new();
    public List<TicketLog> Logs { get; set; } = new();
}
