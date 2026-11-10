using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(int id) =>
        db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.CreatedBy)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<(List<Ticket> Items, int TotalCount)> SearchAsync(TicketFilter filter)
    {
        IQueryable<Ticket> query = db.Tickets;

        if (filter.Status is { } status) query = query.Where(t => t.Status == status);
        if (filter.PriorityId is { } priorityId) query = query.Where(t => t.PriorityId == priorityId);
        if (filter.AssigneeId is { } assigneeId) query = query.Where(t => t.AssigneeId == assigneeId);
        if (filter.CategoryId is { } categoryId) query = query.Where(t => t.CategoryId == categoryId);
        if (filter.CreatedById is { } createdById) query = query.Where(t => t.CreatedById == createdById);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // LIKE ignores case only for the letters A-Z in SQLite. % and _ in the text are searched as plain characters.
            var pattern = $"%{EscapeLike(filter.Search.Trim())}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Title, pattern, LikeEscape) ||
                EF.Functions.Like(t.Description, pattern, LikeEscape));
        }

        var totalCount = await query.CountAsync();

        IOrderedQueryable<Ticket> ordered = filter.SortBy switch
        {
            TicketSortField.Deadline => filter.Descending
                ? query.OrderByDescending(t => t.Deadline)
                : query.OrderBy(t => t.Deadline),
            TicketSortField.Priority => filter.Descending
                ? query.OrderByDescending(t => t.Priority.Level)
                : query.OrderBy(t => t.Priority.Level),
            _ => filter.Descending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt)
        };
        // The id as a last sort key keeps the order stable, so a ticket is never skipped or repeated between pages.
        ordered = filter.Descending ? ordered.ThenByDescending(t => t.Id) : ordered.ThenBy(t => t.Id);

        var items = await ordered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.CreatedBy)
            .Include(t => t.Assignee)
            .AsNoTracking()
            .ToListAsync();
        return (items, totalCount);
    }

    private const string LikeEscape = "\\";

    private static string EscapeLike(string text) =>
        text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task AddAsync(Ticket ticket) => await db.Tickets.AddAsync(ticket);

    public void Remove(Ticket ticket) => db.Tickets.Remove(ticket);
}
