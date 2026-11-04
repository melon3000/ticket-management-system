using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public interface ICommentRepository
{
    /// <summary>Returns the comments of a ticket with their authors, oldest first.</summary>
    Task<List<Comment>> GetByTicketIdAsync(int ticketId);

    /// <summary>Prepares a new comment. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to store it.</summary>
    Task AddAsync(Comment comment);
}
