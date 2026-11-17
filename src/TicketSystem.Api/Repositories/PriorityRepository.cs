using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class PriorityRepository(AppDbContext db) : IPriorityRepository
{
    public Task<List<Priority>> GetAllAsync() =>
        db.Priorities.AsNoTracking().OrderBy(p => p.Level).ToListAsync();

    public async Task<Priority?> GetByIdAsync(int id) => await db.Priorities.FindAsync(id);

    public Task<Priority?> GetNextHigherAsync(int level) =>
        db.Priorities.Where(p => p.Level > level).OrderBy(p => p.Level).FirstOrDefaultAsync();
}
