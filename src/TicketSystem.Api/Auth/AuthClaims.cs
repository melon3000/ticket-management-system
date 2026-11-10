using System.Security.Claims;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Auth;

/// <summary>Claim names used in the JWT.</summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";

    /// <summary>Returns the role of the signed-in user, or null when the token has no valid role.</summary>
    public static UserRole? GetRole(this ClaimsPrincipal user) =>
        Enum.TryParse<UserRole>(user.FindFirst(Role)?.Value, out var role) ? role : null;

    /// <summary>Returns the id of the signed-in user, or null when the token has no valid id.</summary>
    public static int? GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirst(UserId)?.Value, out var id) ? id : null;
}
