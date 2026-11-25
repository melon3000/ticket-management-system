using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Services;

public class UserAdminService(
    IUserRepository users,
    ITicketRepository tickets,
    IUnitOfWork unitOfWork,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    public async Task<List<UserDto>> GetAllAsync()
    {
        var all = await users.GetAllAsync();
        return all.Select(ToDto).ToList();
    }

    public async Task<UserDto> ChangeRoleAsync(int actorId, int userId, UserRole role)
    {
        if (role == UserRole.Service)
            throw new BadRequestException("The Service role belongs to the background service and cannot be given to a person.");

        var user = await users.GetByIdAsync(userId)
                   ?? throw new NotFoundException($"User {userId} was not found.");

        if (user.Role == UserRole.Service)
            throw new ConflictException("The role of the service account cannot be changed.");
        if (user.Id == actorId)
            throw new ConflictException("You cannot change your own role.");
        if (user.Role == role)
            return ToDto(user);

        if (user.Role == UserRole.Support && await tickets.HasUnresolvedAssignedAsync(user.Id))
            throw new ConflictException(
                "The user has unresolved tickets in work. Resolve them before taking the Support role away.");

        var oldRole = user.Role;
        user.Role = role;
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Role of user {UserId} changed from {OldRole} to {NewRole} by user {ActorId}",
            user.Id, oldRole, role, actorId);
        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.DisplayName, user.Role);
}
