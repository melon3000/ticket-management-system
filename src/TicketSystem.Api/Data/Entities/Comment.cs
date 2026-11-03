namespace TicketSystem.Api.Data.Entities;

/// <summary>A message in the conversation between the user and support.</summary>
public class Comment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Text { get; set; } = string.Empty;

    /// <summary>Creation time in UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
