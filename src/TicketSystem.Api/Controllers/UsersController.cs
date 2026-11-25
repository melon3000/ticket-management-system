using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>People that the staff clients need to show, and the user management of administrators.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.SupportOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class UsersController(ILookupService lookupService, IUserAdminService userAdminService) : ControllerBase
{
    /// <summary>Returns the support staff sorted by name. Contains no email addresses.</summary>
    [HttpGet("support")]
    [ProducesResponseType(typeof(List<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupportStaff() => Ok(await lookupService.GetSupportStaffAsync());

    /// <summary>Returns every user with the email address and the role, sorted by name. Administrators only.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await userAdminService.GetAllAsync());

    /// <summary>
    /// Changes the role of a user to User, Support or Admin. Administrators only. The change reaches the person
    /// when they sign in again, because the role is stored in the token.
    /// </summary>
    /// <response code="400">The role is missing, unknown or Service.</response>
    /// <response code="404">The user does not exist.</response>
    /// <response code="409">
    /// The target is the caller or the service account, or a support person still has unresolved tickets in work.
    /// </response>
    [HttpPatch("{id:int}/role")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest request) =>
        Ok(await userAdminService.ChangeRoleAsync(CurrentUserId(), id, request.Role!.Value));

    private int CurrentUserId() =>
        User.GetUserId() ?? throw new ForbiddenException("The token has no valid user id.");
}
