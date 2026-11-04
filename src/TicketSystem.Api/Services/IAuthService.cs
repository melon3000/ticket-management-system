using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

public interface IAuthService
{
    /// <summary>Creates a User account and signs it in. Returns null when the email is already taken.</summary>
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);

    /// <summary>Checks the credentials. Returns null when the email or password is wrong.</summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request);

    /// <summary>Returns the user with the given id, or null.</summary>
    Task<UserDto?> GetUserAsync(int id);
}
