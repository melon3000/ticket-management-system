namespace TicketSystem.Api.Auth;

/// <summary>Settings for creating and validating JWT tokens (section "Jwt" in appsettings).</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TicketSystem.Api";
    public string Audience { get; set; } = "TicketSystem.Clients";

    /// <summary>Secret used to sign tokens. At least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;
}
