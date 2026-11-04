using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(int id) =>
        db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Priority)
            .Include(t => t.CreatedBy)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(Ticket ticket) => await db.Tickets.AddAsync(ticket);

    public void Remove(Ticket ticket) => db.Tickets.Remove(ticket);
}
