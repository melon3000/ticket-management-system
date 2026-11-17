namespace TicketSystem.Api.Dtos;

/// <summary>Average resolution time of one category or one support person.</summary>
/// <param name="Id">Id of the category or of the support person.</param>
/// <param name="Name">Name of the category or the display name of the support person.</param>
/// <param name="ResolvedCount">Number of tickets that are resolved now.</param>
/// <param name="AverageHours">Average time from creation to resolution in hours, or null when nothing is resolved.</param>
public record ResolutionTimeDto(int Id, string Name, int ResolvedCount, double? AverageHours);

/// <summary>Average resolution time of all resolved tickets, by category and by support person.</summary>
public record ResolutionStatisticsDto(
    int ResolvedCount,
    double? AverageHours,
    List<ResolutionTimeDto> ByCategory,
    List<ResolutionTimeDto> BySupport);
