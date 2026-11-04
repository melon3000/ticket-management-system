using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class TicketLogRepository(AppDbContext db) : ITicketLogRepository
{
    public Task<List<TicketLog>> GetByTicketIdAsync(int ticketId) =>
        db.TicketLogs.AsNoTracking()
            .Include(l => l.User)
            .Where(l => l.TicketId == ticketId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(TicketLog log) => await db.TicketLogs.AddAsync(log);
}
