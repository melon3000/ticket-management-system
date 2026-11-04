using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class PriorityRepository(AppDbContext db) : IPriorityRepository
{
    public Task<List<Priority>> GetAllAsync() =>
        db.Priorities.AsNoTracking().OrderBy(p => p.Level).ToListAsync();

    public async Task<Priority?> GetByIdAsync(int id) => await db.Priorities.FindAsync(id);
}
