using System.ComponentModel.DataAnnotations;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Dtos;

/// <summary>Data for creating a new account. New accounts always get the role User.</summary>
public class RegisterRequest
{
    /// <example>jaan.kask@example.com</example>
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <example>Jaan Kask</example>
    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Data for signing in.</summary>
public class LoginRequest
{
    /// <example>admin@ticketsystem.local</example>
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required, MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>A user as shown to API clients. Never contains the password hash.</summary>
public record UserDto(int Id, string Email, string DisplayName, UserRole Role);

/// <summary>Result of a successful registration or login.</summary>
/// <param name="Token">JWT to send as "Authorization: Bearer &lt;token&gt;".</param>
/// <param name="ExpiresAt">When the token stops working (UTC).</param>
public record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
