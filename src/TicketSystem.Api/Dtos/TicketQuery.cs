using System.ComponentModel.DataAnnotations;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Dtos;

/// <summary>Filters, search, sorting and paging of the ticket list. Every part is optional.</summary>
public class TicketQuery
{
    /// <summary>Only tickets in this state: New, InProgress or Resolved.</summary>
    public TicketStatus? Status { get; set; }

    /// <summary>Only tickets with this priority. The id comes from GET /api/priorities.</summary>
    public int? Priority { get; set; }

    /// <summary>Only tickets assigned to this support person. The id is the user id.</summary>
    public int? Assignee { get; set; }

    /// <summary>Only tickets in this category. The id comes from GET /api/categories.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Text to look for in the title or description.</summary>
    [StringLength(100)]
    public string? Search { get; set; }

    /// <summary>Sort field: CreatedAt, Deadline or Priority.</summary>
    public TicketSortField SortBy { get; set; } = TicketSortField.CreatedAt;

    /// <summary>True sorts from the biggest value down, so the newest or most urgent come first.</summary>
    public bool Descending { get; set; } = true;

    /// <summary>Page number, starting from 1.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>Tickets per page, from 1 to 100.</summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
