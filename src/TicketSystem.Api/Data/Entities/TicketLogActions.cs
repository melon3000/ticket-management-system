namespace TicketSystem.Api.Data.Entities;

/// <summary>Allowed values for <see cref="TicketLog.Action"/>.</summary>
public static class TicketLogActions
{
    public const string Created = "Created";
    public const string Assigned = "Assigned";
    public const string StatusChanged = "StatusChanged";
    public const string Escalated = "Escalated";
}
