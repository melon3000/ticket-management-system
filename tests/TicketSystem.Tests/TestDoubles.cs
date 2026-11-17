using TicketSystem.Api.Dtos;
using TicketSystem.Api.Services;

namespace TicketSystem.Tests;

/// <summary>A notifier that sends nothing, for tests that do not care about real-time messages.</summary>
internal sealed class NullTicketNotifier : ITicketNotifier
{
    public Task NotifyAsync(string eventType, TicketDto ticket) => Task.CompletedTask;
}
