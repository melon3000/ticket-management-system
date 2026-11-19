using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

/// <summary>Reference data that clients need for drop-down lists.</summary>
public interface ILookupService
{
    Task<List<CategoryDto>> GetCategoriesAsync();

    Task<List<PriorityDto>> GetPrioritiesAsync();

    /// <summary>Returns the support staff sorted by name, for the assignee filter of the ticket list.</summary>
    Task<List<UserSummaryDto>> GetSupportStaffAsync();
}
