using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

/// <summary>Reference data that clients need for drop-down lists.</summary>
public interface ILookupService
{
    Task<List<CategoryDto>> GetCategoriesAsync();

    Task<List<PriorityDto>> GetPrioritiesAsync();
}
