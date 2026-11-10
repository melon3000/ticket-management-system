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

    /// <summary>Returns a ticket. Ordinary users can only see their own tickets.</summary>
    Task<TicketDto> GetAsync(int id, int userId, UserRole role);

    /// <summary>
    /// Changes the title, description, category or priority. A priority change recalculates the deadline.
    /// The author may edit while the ticket is New, an administrator may edit any unresolved ticket.
    /// </summary>
    Task<TicketDto> UpdateAsync(int id, UpdateTicketRequest request, int userId, UserRole role);

    /// <summary>Deletes a ticket together with its comments and log.</summary>
    Task DeleteAsync(int id);
}
