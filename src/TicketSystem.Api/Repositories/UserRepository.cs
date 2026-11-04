using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Data;
using TicketSystem.Api.Data.Entities;

namespace TicketSystem.Api.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id) => await db.Users.FindAsync(id);

    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<List<User>> GetByRoleAsync(UserRole role) =>
        db.Users.AsNoTracking().Where(u => u.Role == role).OrderBy(u => u.DisplayName).ToListAsync();

    public Task<bool> EmailExistsAsync(string email) => db.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(User user) => await db.Users.AddAsync(user);
}
