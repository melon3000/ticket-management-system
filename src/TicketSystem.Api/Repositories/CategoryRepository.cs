using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync() =>
        db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

    public async Task<Category?> GetByIdAsync(int id) => await db.Categories.FindAsync(id);
}
