namespace TicketSystem.Api.Data.Entities;

/// <summary>A person or technical account that can log in.</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Login name. Unique.</summary>
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Hashed password. Never returned from the API.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>Creation time in UTC.</summary>
    public DateTime CreatedAt { get; set; }

    public List<Ticket> CreatedTickets { get; set; } = new();
    public List<Ticket> AssignedTickets { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<TicketLog> Logs { get; set; } = new();
}
