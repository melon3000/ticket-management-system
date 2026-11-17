using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TicketSystem.Worker;

/// <summary>Calls the ticket API with HttpClient. Signs in with the Service account before the first call.</summary>
public class TicketApiClient(HttpClient http, IOptions<ApiOptions> options) : ITicketApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private bool _signedIn;

    public async Task<IReadOnlyList<OverdueTicket>> GetOverdueAsync(CancellationToken cancellationToken)
    {
        await EnsureSignedInAsync(cancellationToken);
        var tickets = await http.GetFromJsonAsync<List<OverdueTicket>>("api/tickets/overdue", Json, cancellationToken);
        return tickets ?? [];
    }

    public async Task<bool> EscalateAsync(int ticketId, CancellationToken cancellationToken)
    {
        await EnsureSignedInAsync(cancellationToken);
        using var response = await http.PatchAsync($"api/tickets/{ticketId}/escalate", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task EnsureSignedInAsync(CancellationToken cancellationToken)
    {
        if (_signedIn)
        {
            return;
        }

        var settings = options.Value;
        using var response = await http.PostAsJsonAsync(
            "api/auth/login", new { email = settings.Email, password = settings.Password }, Json, cancellationToken);
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResult>(Json, cancellationToken)
                    ?? throw new InvalidOperationException("The API returned an empty login response.");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        _signedIn = true;
    }
}
