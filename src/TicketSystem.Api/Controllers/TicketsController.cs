using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>Creating, reading, editing and deleting tickets.</summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class TicketsController(ITicketService ticketService) : ControllerBase
{
    /// <summary>Creates a ticket. The deadline is calculated from the priority.</summary>
    /// <response code="201">The ticket was created.</response>
    /// <response code="400">The data is not valid or the category or priority does not exist.</response>
    [HttpPost]
    [Authorize(Roles = Roles.TicketAuthors)]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateTicketRequest request)
    {
        var ticket = await ticketService.CreateAsync(request, CurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    /// <summary>Returns one ticket. Ordinary users can only see their own tickets.</summary>
    /// <response code="403">An ordinary user asked for someone else's ticket.</response>
    /// <response code="404">The ticket does not exist.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id) =>
        Ok(await ticketService.GetAsync(id, CurrentUserId(), CurrentRole()));

    /// <summary>Edits a ticket. A new priority recalculates the deadline.</summary>
    /// <response code="403">Only the author of a new ticket or an administrator may edit.</response>
    /// <response code="404">The ticket does not exist.</response>
    /// <response code="409">The ticket is already resolved.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.TicketAuthors)]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateTicketRequest request) =>
        Ok(await ticketService.UpdateAsync(id, request, CurrentUserId(), CurrentRole()));

    /// <summary>Deletes a ticket with its comments and log. Administrators only.</summary>
    /// <response code="204">The ticket was deleted.</response>
    /// <response code="404">The ticket does not exist.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await ticketService.DeleteAsync(id);
        return NoContent();
    }

    private int CurrentUserId() =>
        User.GetUserId() ?? throw new ForbiddenException("The token has no valid user id.");

    private Data.Entities.UserRole CurrentRole() =>
        User.GetRole() ?? throw new ForbiddenException("The token has no valid role.");
}
