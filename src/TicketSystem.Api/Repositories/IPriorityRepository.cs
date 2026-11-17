using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface IPriorityRepository
{
    /// <summary>Returns all priorities sorted from the lowest to the highest level.</summary>
    Task<List<Priority>> GetAllAsync();

    Task<Priority?> GetByIdAsync(int id);

    /// <summary>
    /// Returns the priority with the smallest level above <paramref name="level"/>, or null when there is none.
    /// The result is tracked by the context, so a ticket can be pointed at it directly.
    /// </summary>
    Task<Priority?> GetNextHigherAsync(int level);
}
