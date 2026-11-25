using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Repositories;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

public class AuthServiceTests
{
    private static readonly JwtOptions Jwt = new()
    {
        Key = "test-signing-key-with-at-least-32-characters-long",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiresMinutes = 30
    };

    private static (SqliteConnection Connection, AppDbContext Db, AuthService Service) CreateService()
    {
        var (connection, db) = TestDatabase.CreateSeeded();

        var service = new AuthService(
            new UserRepository(db),
            new UnitOfWork(db),
            new PasswordHasher<User>(),
            new JwtTokenService(Options.Create(Jwt)),
            NullLogger<AuthService>.Instance);
        return (connection, db, service);
    }

    [Fact]
    public async Task Register_CreatesUserWithRoleUserAndHashedPassword()
    {
        var (connection, db, service) = CreateService();
        using var _ = connection;

        var response = await service.RegisterAsync(
            new RegisterRequest { Email = "  New.User@Example.com ", DisplayName = "New User", Password = "Secret123!" });

        Assert.NotNull(response);
        Assert.Equal(UserRole.User, response.User.Role);
        var stored = await db.Users.SingleAsync(u => u.Email == "new.user@example.com");
        Assert.NotEqual("Secret123!", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_ReturnsNullWhenEmailAlreadyExists()
    {
        var (connection, _, service) = CreateService();
        using var _ = connection;

        var response = await service.RegisterAsync(
            new RegisterRequest { Email = "ADMIN@ticketsystem.local", DisplayName = "Copy", Password = "Secret123!" });

        Assert.Null(response);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokenWithRoleClaim()
    {
        var (connection, _, service) = CreateService();
        using var _ = connection;

        var response = await service.LoginAsync(
            new LoginRequest { Email = "mari.maasikas@ticketsystem.local", Password = "Password123!" });

        Assert.NotNull(response);
        var token = new JsonWebToken(response.Token);
        Assert.Equal("Support", token.Claims.First(c => c.Type == AuthClaims.Role).Value);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_ReturnsNull()
    {
        var (connection, _, service) = CreateService();
        using var _ = connection;

        var wrongPassword = await service.LoginAsync(
            new LoginRequest { Email = "admin@ticketsystem.local", Password = "wrong-password" });
        var unknownEmail = await service.LoginAsync(
            new LoginRequest { Email = "nobody@example.com", Password = "Password123!" });

        Assert.Null(wrongPassword);
        Assert.Null(unknownEmail);
    }
}
