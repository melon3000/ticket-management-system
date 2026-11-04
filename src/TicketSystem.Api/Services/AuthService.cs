using Microsoft.AspNetCore.Identity;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Services;

public class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email))
        {
            logger.LogInformation("Registration refused: email {Email} is already in use", email);
            return null;
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("User {UserId} registered", user.Id);

        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email));
        if (user is null)
        {
            logger.LogWarning("Login failed: unknown email");
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
            return null;
        }

        logger.LogInformation("User {UserId} signed in", user.Id);
        return CreateResponse(user);
    }

    public async Task<UserDto?> GetUserAsync(int id)
    {
        var user = await users.GetByIdAsync(id);
        return user is null ? null : ToDto(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToDto(user));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.DisplayName, user.Role);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
