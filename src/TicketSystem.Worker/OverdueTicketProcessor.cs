using Microsoft.Extensions.Logging;

namespace TicketSystem.Worker;

/// <summary>
/// One check for late tickets: asks the API for them and tells the API to escalate those that were not escalated
/// yet. The rules of escalation (the new priority, the change log) live in the API, the Worker only starts them.
/// </summary>
public class OverdueTicketProcessor(ITicketApiClient api, ILogger<OverdueTicketProcessor> logger)
{
    /// <summary>Runs the check once and returns the number of tickets that were escalated.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var overdue = await api.GetOverdueAsync(cancellationToken);
        var waiting = overdue.Where(t => !t.IsOverdue).ToList();
        logger.LogInformation("{Total} tickets are past their deadline, {Waiting} of them are not escalated yet.",
            overdue.Count, waiting.Count);

        var escalated = 0;
        foreach (var ticket in waiting)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await api.EscalateAsync(ticket.Id, cancellationToken))
                {
                    escalated++;
                    logger.LogInformation("Ticket {TicketId} \"{Title}\" was escalated.", ticket.Id, ticket.Title);
                }
                else
                {
                    logger.LogInformation("Ticket {TicketId} was skipped, the API did not allow escalating it.", ticket.Id);
                }
            }
            catch (HttpRequestException ex)
            {
                // One failed ticket must not stop the others. It is tried again at the next check.
                logger.LogWarning(ex, "Escalating ticket {TicketId} failed.", ticket.Id);
            }
        }

        return escalated;
    }
}
