namespace TicketSystem.Api.Repositories;

/// <summary>
/// Saves all changes made through the repositories in one step.
/// Repositories only prepare changes, the service decides when to save.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Writes all pending changes to the database and returns the number of changed rows.</summary>
    Task<int> SaveChangesAsync();
}
