using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

/// <summary>Lets an administrator see the users and change their roles.</summary>
public interface IUserAdminService
{
    /// <summary>Returns every user with the email address and the role, sorted by name.</summary>
    Task<List<UserDto>> GetAllAsync();

    /// <summary>
    /// Changes the role of a user.
    /// An administrator cannot change their own role, the role of the service account cannot be changed or given,
    /// and a support person with unresolved tickets in work stays support until they are resolved.
    /// </summary>
    /// <param name="actorId">The administrator who makes the change.</param>
    /// <param name="userId">The user whose role changes.</param>
    /// <param name="role">The new role.</param>
    Task<UserDto> ChangeRoleAsync(int actorId, int userId, UserRole role);
}
