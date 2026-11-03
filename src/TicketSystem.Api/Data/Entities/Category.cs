namespace TicketSystem.Api.Data.Entities;

/// <summary>Kind of problem, for example hardware or network.</summary>
public class Category
{
    public int Id { get; set; }

    /// <summary>Category name. Unique.</summary>
    public string Name { get; set; } = string.Empty;

    public List<Ticket> Tickets { get; set; } = new();
}
