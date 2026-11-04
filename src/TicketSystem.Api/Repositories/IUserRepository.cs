using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);

    /// <summary>Finds a user by email. The email must already be in lower case.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Returns the users with the given role, sorted by display name.</summary>
    Task<List<User>> GetByRoleAsync(UserRole role);

    Task<bool> EmailExistsAsync(string email);

    /// <summary>Prepares a new user. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to store it.</summary>
    Task AddAsync(User user);
}
