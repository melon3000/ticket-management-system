using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>Figures for the administrator.</summary>
[ApiController]
[Route("api/statistics")]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class StatisticsController(IStatisticsService statistics) : ControllerBase
{
    /// <summary>
    /// Returns the average time from creation to resolution, in hours, for every category and every support person.
    /// Only tickets that are resolved now are counted. A reopened ticket is left out until it is resolved again.
    /// </summary>
    [HttpGet("resolution-time")]
    [ProducesResponseType(typeof(ResolutionStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResolutionStatisticsDto>> GetResolutionTimes() =>
        await statistics.GetResolutionTimesAsync();
}
