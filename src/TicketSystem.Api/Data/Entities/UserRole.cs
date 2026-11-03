namespace TicketSystem.Api.Data.Entities;

/// <summary>Role of a user. Stored in the database as text.</summary>
public enum UserRole
{
    /// <summary>Regular user who reports problems.</summary>
    User,

    /// <summary>Support staff member who resolves tickets.</summary>
    Support,

    /// <summary>Administrator who manages everything and sees statistics.</summary>
    Admin,

    /// <summary>Technical account used by the background Worker.</summary>
    Service
}
