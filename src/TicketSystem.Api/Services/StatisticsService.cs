using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Services;

public class StatisticsService(
    ITicketRepository tickets,
    ICategoryRepository categories,
    IUserRepository users) : IStatisticsService
{
    public async Task<ResolutionStatisticsDto> GetResolutionTimesAsync()
    {
        var resolved = await tickets.GetResolvedAsync();
        var allCategories = await categories.GetAllAsync();
        var supportStaff = await users.GetByRoleAsync(UserRole.Support);

        var byCategory = allCategories
            .Select(c => Summarize(c.Id, c.Name, resolved.Where(r => r.CategoryId == c.Id)))
            .ToList();
        var bySupport = supportStaff
            .Select(u => Summarize(u.Id, u.DisplayName, resolved.Where(r => r.AssigneeId == u.Id)))
            .ToList();

        var total = Summarize(0, string.Empty, resolved);
        return new ResolutionStatisticsDto(total.ResolvedCount, total.AverageHours, byCategory, bySupport);
    }

    private static ResolutionTimeDto Summarize(int id, string name, IEnumerable<ResolvedTicketRow> rows)
    {
        var durations = rows.Select(r => (r.ResolvedAt - r.CreatedAt).TotalHours).ToList();
        double? average = durations.Count == 0 ? null : Math.Round(durations.Average(), 2);
        return new ResolutionTimeDto(id, name, durations.Count, average);
    }
}
