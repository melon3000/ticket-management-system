namespace TicketSystem.Api.Dtos;

/// <summary>A ticket priority as shown to API clients.</summary>
/// <param name="Level">Order of importance, 1 is the lowest.</param>
/// <param name="DeadlineHours">Hours between creation and the deadline.</param>
public record PriorityDto(int Id, string Name, int Level, int DeadlineHours);
