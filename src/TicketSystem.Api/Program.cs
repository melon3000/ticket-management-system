using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicketSystem.Api.Auth;
using TicketSystem.Api.Data;
using TicketSystem.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    // Send enums (for example the user role) as text like "Admin" instead of numbers.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// SQLite database. Only the API talks to it.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Layers: controllers -> services -> repositories -> database.
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

// JWT authentication. The signing key comes from configuration (never hard-coded in release builds).
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured and at least 32 characters long.");
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim names from the token ("sub", "role") instead of remapping them.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AuthClaims.Name,
            RoleClaimType = AuthClaims.Role
        };

        // A browser cannot set the Authorization header of a WebSocket, so the WebSocket channel
        // also accepts the token from the query string: /ws/tickets?access_token=...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(TicketWebSocketExtensions.Path))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorizationPolicies();

// Errors are returned as ProblemDetails (RFC 9457) by one global handler.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// CORS: only the React client origins from configuration may call the API from a browser.
const string CorsPolicyName = "ReactClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Swagger / OpenAPI documentation with the Authorize button.
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Create or update the database and add the starting data on the first start.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();   // unhandled exceptions -> ProblemDetails
app.UseStatusCodePages();    // empty 401/403/404 responses -> ProblemDetails
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}
else
{
    // In development the API runs over plain http, so there is no https port to redirect to.
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);

// WebSockets: only the React client origins from configuration may open a connection from a browser.
var webSocketOptions = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) };
foreach (var origin in allowedOrigins)
{
    webSocketOptions.AllowedOrigins.Add(origin);
}

app.UseWebSockets(webSocketOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapTicketWebSocket();

app.Run();
