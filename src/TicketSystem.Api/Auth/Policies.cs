using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Auth;

/// <summary>Authorization policy names and their registration.</summary>
public static class Policies
{
    /// <summary>Support staff or administrators.</summary>
    public const string SupportOrAdmin = "SupportOrAdmin";

    /// <summary>Administrators only.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>The background Worker Service account only.</summary>
    public const string ServiceOnly = "ServiceOnly";

    /// <summary>Support staff, administrators and the Worker Service.</summary>
    public const string StaffOrService = "StaffOrService";

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(SupportOrAdmin, p => p.RequireRole(nameof(UserRole.Support), nameof(UserRole.Admin)))
            .AddPolicy(AdminOnly, p => p.RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(ServiceOnly, p => p.RequireRole(nameof(UserRole.Service)))
            .AddPolicy(StaffOrService, p => p.RequireRole(
                nameof(UserRole.Support), nameof(UserRole.Admin), nameof(UserRole.Service)));
        return services;
    }
}
