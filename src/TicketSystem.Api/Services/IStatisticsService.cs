using TicketSystem.Api.Dtos;

namespace TicketSystem.Api.Services;

/// <summary>Figures about how fast tickets are resolved.</summary>
public interface IStatisticsService
{
    /// <summary>
    /// Calculates the average time from creation to resolution over all resolved tickets,
    /// for every category and for every support person.
    /// </summary>
    Task<ResolutionStatisticsDto> GetResolutionTimesAsync();
}
