using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    public Task<List<Comment>> GetByTicketIdAsync(int ticketId) =>
        db.Comments.AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync();

    public async Task AddAsync(Comment comment) => await db.Comments.AddAsync(comment);
}
