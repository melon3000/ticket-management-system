using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Mapping;

/// <summary>Converts entities to DTOs. Entities are never sent to clients directly.</summary>
public static class TicketMapper
{
    /// <summary>Converts a ticket whose category, priority, author and assignee are loaded.</summary>
    public static TicketDto ToDto(this Ticket ticket) => new(
        ticket.Id,
        ticket.Title,
        ticket.Description,
        ticket.Status,
        new CategoryDto(ticket.Category.Id, ticket.Category.Name),
        new PriorityDto(ticket.Priority.Id, ticket.Priority.Name, ticket.Priority.Level, ticket.Priority.DeadlineHours),
        ticket.CreatedBy.ToSummary(),
        ticket.Assignee?.ToSummary(),
        ticket.CreatedAt,
        ticket.Deadline,
        ticket.ResolvedAt,
        ticket.IsOverdue);

    public static UserSummaryDto ToSummary(this User user) => new(user.Id, user.DisplayName);
}
