using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Services;

namespace TicketSystem.Api.Controllers;

/// <summary>The conversation between the user and support on a ticket.</summary>
[ApiController]
[Route("api/tickets/{ticketId:int}/comments")]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    /// <summary>Adds a comment to a ticket. Ordinary users can only comment on their own tickets.</summary>
    /// <response code="201">The comment was added.</response>
    /// <response code="400">The text is missing or too short or too long.</response>
    /// <response code="403">An ordinary user tried to comment on someone else's ticket.</response>
    /// <response code="404">The ticket does not exist.</response>
    [HttpPost]
    [Authorize(Roles = Roles.TicketAuthors)]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(int ticketId, CreateCommentRequest request)
    {
        var comment = await commentService.AddAsync(ticketId, request, CurrentUserId(), CurrentRole());
        return CreatedAtAction(nameof(Get), new { ticketId }, comment);
    }

    /// <summary>Returns the comments of a ticket, oldest first. Ordinary users can only see their own tickets.</summary>
    /// <response code="403">An ordinary user asked for the comments of someone else's ticket.</response>
    /// <response code="404">The ticket does not exist.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int ticketId) =>
        Ok(await commentService.GetAsync(ticketId, CurrentUserId(), CurrentRole()));

    private int CurrentUserId() =>
        User.GetUserId() ?? throw new ForbiddenException("The token has no valid user id.");

    private Data.Entities.UserRole CurrentRole() =>
        User.GetRole() ?? throw new ForbiddenException("The token has no valid role.");
}
