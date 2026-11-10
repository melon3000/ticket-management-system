namespace TicketSystem.Api.Auth;

/// <summary>Role lists for the [Authorize(Roles = ...)] attribute. The names match <c>UserRole</c>.</summary>
public static class Roles
{
    /// <summary>Everyone who may report tickets. The Worker Service account may not.</summary>
    public const string TicketAuthors = "User,Support,Admin";

    public const string Admin = "Admin";
}
