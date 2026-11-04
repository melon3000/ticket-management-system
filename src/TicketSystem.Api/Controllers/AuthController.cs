using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>Registration, sign-in and the current user.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Creates a new account with the role User and returns a token.</summary>
    /// <response code="200">The account was created.</response>
    /// <response code="400">The data is not valid.</response>
    /// <response code="409">The email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var response = await authService.RegisterAsync(request);
        return response is null
            ? Problem(title: "Email is already registered", statusCode: StatusCodes.Status409Conflict)
            : Ok(response);
    }

    /// <summary>Signs in with email and password and returns a token.</summary>
    /// <response code="200">The credentials are correct.</response>
    /// <response code="401">The email or password is wrong.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);
        return response is null
            ? Problem(title: "Invalid email or password", statusCode: StatusCodes.Status401Unauthorized)
            : Ok(response);
    }

    /// <summary>Returns the signed-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var id = User.GetUserId();
        var user = id is null ? null : await authService.GetUserAsync(id.Value);
        return user is null ? Unauthorized() : Ok(user);
    }
}
