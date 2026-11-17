using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Mapping;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Services;

public class CommentService(
    ITicketRepository tickets,
    ICommentRepository comments,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<CommentService> logger) : ICommentService
{
    public async Task<CommentDto> AddAsync(int ticketId, CreateCommentRequest request, int userId, UserRole role)
    {
        var ticket = await GetAccessibleTicketAsync(ticketId, userId, role);
        var author = await users.GetByIdAsync(userId)
                     ?? throw new ForbiddenException("The signed-in user no longer exists.");

        var comment = new Comment
        {
            TicketId = ticket.Id,
            Author = author,
            Text = request.Text.Trim(),
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        await comments.AddAsync(comment);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("User {UserId} commented on ticket {TicketId}", userId, ticketId);
        return comment.ToDto();
    }

    public async Task<List<CommentDto>> GetAsync(int ticketId, int userId, UserRole role)
    {
        await GetAccessibleTicketAsync(ticketId, userId, role);
        var list = await comments.GetByTicketIdAsync(ticketId);
        return list.Select(c => c.ToDto()).ToList();
    }

    private async Task<Ticket> GetAccessibleTicketAsync(int ticketId, int userId, UserRole role)
    {
        var ticket = await tickets.GetByIdAsync(ticketId)
                     ?? throw new NotFoundException($"Ticket {ticketId} was not found.");
        if (role == UserRole.User && ticket.CreatedById != userId)
        {
            throw new ForbiddenException("You can only use the comments of your own tickets.");
        }

        return ticket;
    }
}
