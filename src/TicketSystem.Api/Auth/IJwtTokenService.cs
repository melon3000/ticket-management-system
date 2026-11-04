using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Auth;

public interface IJwtTokenService
{
    /// <summary>Creates a signed token with the user's id, email, name and role.</summary>
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
