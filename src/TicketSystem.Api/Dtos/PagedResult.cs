namespace TicketSystem.Api.Dtos;

/// <summary>One page of a longer list.</summary>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">Page number, starting from 1.</param>
/// <param name="PageSize">Maximum number of items on a page.</param>
/// <param name="TotalCount">Number of items on all pages together.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Number of pages. Zero when the list is empty.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
