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

    /// <summary>Converts a log entry whose user is loaded.</summary>
    public static TicketLogDto ToDto(this TicketLog log) =>
        new(log.Id, log.Action, log.OldValue, log.NewValue, log.User.ToSummary(), log.CreatedAt);

    /// <summary>Converts a comment whose author is loaded.</summary>
    public static CommentDto ToDto(this Comment comment) =>
        new(comment.Id, comment.Text, comment.Author.ToSummary(), comment.CreatedAt);

    public static UserSummaryDto ToSummary(this User user) => new(user.Id, user.DisplayName);
}
