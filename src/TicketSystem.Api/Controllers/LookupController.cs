using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>Reference data for drop-down lists. The controller only passes the work on to the service.</summary>
[ApiController]
[Route("api")]
public class LookupController(ILookupService lookupService) : ControllerBase
{
    /// <summary>Returns all ticket categories sorted by name.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories() => Ok(await lookupService.GetCategoriesAsync());

    /// <summary>Returns all ticket priorities from the lowest to the highest level.</summary>
    [HttpGet("priorities")]
    [ProducesResponseType(typeof(List<PriorityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriorities() => Ok(await lookupService.GetPrioritiesAsync());
}
