using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface ICategoryRepository
{
    /// <summary>Returns all categories sorted by name.</summary>
    Task<List<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);
}
