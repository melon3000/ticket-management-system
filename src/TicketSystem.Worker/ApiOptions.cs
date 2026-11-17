namespace TicketSystem.Worker;

/// <summary>Where the ticket API is and which account the Worker uses to sign in to it.</summary>
public class ApiOptions
{
    public const string SectionName = "TicketApi";

    /// <summary>Address of the ticket API.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5119";

    /// <summary>Email of the account with the Service role.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Password of the Service account. Set it through user secrets or an environment variable outside development.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Minutes between two checks for overdue tickets.</summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>True when every setting is usable.</summary>
    public bool IsValid() =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Password)
        && IntervalMinutes is >= 1 and <= 1440;
}
