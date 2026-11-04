using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface IPriorityRepository
{
    /// <summary>Returns all priorities sorted from the lowest to the highest level.</summary>
    Task<List<Priority>> GetAllAsync();

    Task<Priority?> GetByIdAsync(int id);
}
