using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

public interface ITicketService
{
    /// <summary>
    /// Creates a ticket for the user. The deadline is the creation time plus the hours of the chosen priority.
    /// Writes a "Created" entry to the change log.
    /// </summary>
    Task<TicketDto> CreateAsync(CreateTicketRequest request, int userId);

    /// <summary>
    /// Returns one page of tickets with the filters, search and sorting of the query.
    /// Ordinary users only get their own tickets, support staff and administrators get all of them.
    /// </summary>
    Task<PagedResult<TicketDto>> ListAsync(TicketQuery query, int userId, UserRole role);

    /// <summary>Returns a ticket. Ordinary users can only see their own tickets.</summary>
    Task<TicketDto> GetAsync(int id, int userId, UserRole role);

    /// <summary>
    /// Changes the title, description, category or priority. A priority change recalculates the deadline.
    /// The author may edit while the ticket is New, an administrator may edit any unresolved ticket.
    /// </summary>
    Task<TicketDto> UpdateAsync(int id, UpdateTicketRequest request, int userId, UserRole role);

    /// <summary>
    /// A support person takes a new ticket into work: they become the assignee and the ticket becomes InProgress.
    /// Writes "Assigned" and "StatusChanged" entries to the change log.
    /// </summary>
    Task<TicketDto> AssignAsync(int id, int userId);

    /// <summary>
    /// Changes the state: InProgress to Resolved (sets the resolution time) or Resolved back to InProgress.
    /// Only the assignee or an administrator may do it. Writes a "StatusChanged" entry to the change log.
    /// </summary>
    Task<TicketDto> ChangeStatusAsync(int id, TicketStatus status, int userId, UserRole role);

    /// <summary>Returns the change log of a ticket, oldest first. Ordinary users can only see their own tickets.</summary>
    Task<List<TicketLogDto>> GetLogAsync(int id, int userId, UserRole role);

    /// <summary>Deletes a ticket together with its comments and log.</summary>
    Task DeleteAsync(int id);
}
