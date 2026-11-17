using Microsoft.Extensions.Options;
using TicketSystem.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .Validate(o => o.IsValid(),
        "TicketApi settings are not valid: BaseUrl must be a full address, Email and Password must be set, " +
        "IntervalMinutes must be between 1 and 1440.")
    .ValidateOnStart();

builder.Services.AddHttpClient<ITicketApiClient, TicketApiClient>((services, client) =>
{
    var settings = services.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<OverdueTicketProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
