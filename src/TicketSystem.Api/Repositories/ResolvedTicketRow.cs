namespace TicketSystem.Api.Repositories;

/// <summary>The few fields of a resolved ticket that the statistics need.</summary>
public record ResolvedTicketRow(int CategoryId, int? AssigneeId, DateTime CreatedAt, DateTime ResolvedAt);
