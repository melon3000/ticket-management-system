using System.ComponentModel.DataAnnotations;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Dtos;

/// <summary>Data for creating a ticket. Status, deadline and author are set by the server.</summary>
public class CreateTicketRequest
{
    /// <example>Printer ei tööta</example>
    [Required, StringLength(100, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    /// <example>Kolmanda korruse printer ei prindi ja näitab veateadet E-502.</example>
    [Required, StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Id of a category from GET /api/categories.</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    /// <summary>Id of a priority from GET /api/priorities. The priority sets the deadline.</summary>
    /// <example>2</example>
    [Range(1, int.MaxValue, ErrorMessage = "Priority is required.")]
    public int PriorityId { get; set; }
}

/// <summary>Data for changing a ticket. Same rules as when creating it.</summary>
public class UpdateTicketRequest : CreateTicketRequest;

/// <summary>A person shown on a ticket. Contains no email, so ordinary users cannot read other people's addresses.</summary>
public record UserSummaryDto(int Id, string DisplayName);

/// <summary>A ticket as shown to API clients.</summary>
/// <param name="Id">Ticket id.</param>
/// <param name="Title">Short title.</param>
/// <param name="Description">Full description of the problem.</param>
/// <param name="Status">New, InProgress or Resolved.</param>
/// <param name="Category">The category of the ticket.</param>
/// <param name="Priority">The priority of the ticket.</param>
/// <param name="CreatedBy">The user who created the ticket.</param>
/// <param name="Assignee">The support person who handles the ticket, or null while it is unassigned.</param>
/// <param name="CreatedAt">When the ticket was created (UTC).</param>
/// <param name="Deadline">When the ticket should be resolved (UTC).</param>
/// <param name="ResolvedAt">When the ticket was resolved (UTC), or null while it is open.</param>
/// <param name="IsOverdue">True after the Worker Service has escalated the ticket for missing its deadline.</param>
public record TicketDto(
    int Id,
    string Title,
    string Description,
    TicketStatus Status,
    CategoryDto Category,
    PriorityDto Priority,
    UserSummaryDto CreatedBy,
    UserSummaryDto? Assignee,
    DateTime CreatedAt,
    DateTime Deadline,
    DateTime? ResolvedAt,
    bool IsOverdue);

/// <summary>The new state of a ticket. A ticket changes from InProgress to Resolved or back; New is set by the system.</summary>
public class ChangeStatusRequest
{
    /// <example>Resolved</example>
    [Required, EnumDataType(typeof(TicketStatus))]
    public TicketStatus? Status { get; set; }
}

/// <summary>One entry of the change log of a ticket.</summary>
/// <param name="Id">Log entry id.</param>
/// <param name="Action">Created, Assigned, StatusChanged or Escalated.</param>
/// <param name="OldValue">Value before the change, or null when there was none.</param>
/// <param name="NewValue">Value after the change.</param>
/// <param name="User">Who made the change.</param>
/// <param name="CreatedAt">When the change was made (UTC).</param>
public record TicketLogDto(int Id, string Action, string? OldValue, string? NewValue, UserSummaryDto User, DateTime CreatedAt);
