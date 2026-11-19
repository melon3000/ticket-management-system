using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>People that the clients of the staff need to show, for example in the filter of the ticket list.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.SupportOrAdmin)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class UsersController(ILookupService lookupService) : ControllerBase
{
    /// <summary>Returns the support staff sorted by name. Contains no email addresses.</summary>
    [HttpGet("support")]
    [ProducesResponseType(typeof(List<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupportStaff() => Ok(await lookupService.GetSupportStaffAsync());
}
