using Microsoft.AspNetCore.Identity;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Infrastructure;

/// <summary>Registers the application layers in dependency injection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the repositories. One instance per request, sharing the same database context.</summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IPriorityRepository, PriorityRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ITicketLogRepository, TicketLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>Registers the services that hold the business logic.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<WebSocketConnections>();
        services.AddSingleton<ITicketNotifier, TicketNotifier>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        return services;
    }
}
