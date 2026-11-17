using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

public interface ICommentService
{
    /// <summary>
    /// Adds a comment to a ticket. Ordinary users can only comment on their own tickets,
    /// support staff and administrators can comment on any ticket.
    /// </summary>
    Task<CommentDto> AddAsync(int ticketId, CreateCommentRequest request, int userId, UserRole role);

    /// <summary>Returns the comments of a ticket, oldest first. Ordinary users can only see their own tickets.</summary>
    Task<List<CommentDto>> GetAsync(int ticketId, int userId, UserRole role);
}
