using TicketSystem.Api.Dtos;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Services;

public class LookupService(ICategoryRepository categories, IPriorityRepository priorities) : ILookupService
{
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var items = await categories.GetAllAsync();
        return items.Select(c => new CategoryDto(c.Id, c.Name)).ToList();
    }

    public async Task<List<PriorityDto>> GetPrioritiesAsync()
    {
        var items = await priorities.GetAllAsync();
        return items.Select(p => new PriorityDto(p.Id, p.Name, p.Level, p.DeadlineHours)).ToList();
    }
}
