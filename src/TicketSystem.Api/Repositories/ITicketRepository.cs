using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface ITicketRepository
{
    /// <summary>Returns a ticket with its category, priority, author and assignee, or null when it does not exist.</summary>
    Task<Ticket?> GetByIdAsync(int id);

    /// <summary>
    /// Finds tickets that match the filter, sorted and cut to one page.
    /// Returns the tickets of the page and the number of matching tickets on all pages.
    /// </summary>
    Task<(List<Ticket> Items, int TotalCount)> SearchAsync(TicketFilter filter);

    /// <summary>
    /// Returns the unresolved tickets whose deadline is before <paramref name="now"/>, the oldest deadline first.
    /// Tickets that were already escalated are included.
    /// </summary>
    Task<List<Ticket>> GetOverdueAsync(DateTime now);

    /// <summary>Tells whether the user is the assignee of a ticket that is not resolved yet.</summary>
    Task<bool> HasUnresolvedAssignedAsync(int userId);

    /// <summary>Returns the creation and resolution time of every ticket that is resolved now.</summary>
    Task<List<ResolvedTicketRow>> GetResolvedAsync();

    /// <summary>Prepares a new ticket. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to store it.</summary>
    Task AddAsync(Ticket ticket);

    /// <summary>Prepares the removal of a ticket together with its comments and log.</summary>
    void Remove(Ticket ticket);
}
