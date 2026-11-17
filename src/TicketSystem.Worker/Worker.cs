using Microsoft.Extensions.Options;

namespace TicketSystem.Worker;

/// <summary>Runs the check for overdue tickets right after the start and then at every tick of a PeriodicTimer.</summary>
public class Worker(IServiceScopeFactory scopes, IOptions<ApiOptions> options, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.IntervalMinutes);
        logger.LogInformation("Worker started. Checking for overdue tickets every {Interval}.", interval);

        try
        {
            using var timer = new PeriodicTimer(interval);
            await CheckAsync(stoppingToken);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker is stopping.");
        }
    }

    private async Task CheckAsync(CancellationToken stoppingToken)
    {
        try
        {
            // A new scope gives every check its own HttpClient and a fresh sign-in.
            using var scope = scopes.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<OverdueTicketProcessor>();
            var escalated = await processor.RunOnceAsync(stoppingToken);
            logger.LogInformation("Check finished, {Count} tickets escalated.", escalated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            // For example the API is not running. The Worker keeps living and tries again at the next tick.
            logger.LogError(ex, "The check for overdue tickets failed. The next check is in {Minutes} minutes.",
                options.Value.IntervalMinutes);
        }
    }
}
