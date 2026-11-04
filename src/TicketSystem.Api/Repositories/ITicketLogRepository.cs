using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface ITicketLogRepository
{
    /// <summary>Returns the change log of a ticket with the users who made the changes, oldest first.</summary>
    Task<List<TicketLog>> GetByTicketIdAsync(int ticketId);

    /// <summary>Prepares a new log entry. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to store it.</summary>
    Task AddAsync(TicketLog log);
}
