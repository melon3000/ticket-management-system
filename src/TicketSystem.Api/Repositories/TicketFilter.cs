using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

/// <summary>What the ticket list can be sorted by.</summary>
public enum TicketSortField
{
    /// <summary>Creation time.</summary>
    CreatedAt,

    /// <summary>Deadline.</summary>
    Deadline,

    /// <summary>Priority level: 1 is the lowest.</summary>
    Priority
}

/// <summary>Conditions, sorting and paging for a ticket search. Empty conditions are ignored.</summary>
public record TicketFilter(
    TicketStatus? Status,
    int? PriorityId,
    int? AssigneeId,
    int? CategoryId,
    string? Search,
    int? CreatedById,
    TicketSortField SortBy,
    bool Descending,
    int Page,
    int PageSize);
