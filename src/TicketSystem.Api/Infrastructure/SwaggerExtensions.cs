using Microsoft.OpenApi;

namespace TicketSystem.Api.Infrastructure;

/// <summary>Swagger (OpenAPI) setup: API description, XML comments and the Authorize button for JWT.</summary>
public static class SwaggerExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Ticket System API",
                Version = "v1",
                Description = "Web API of the support ticket system. " +
                              "Sign in with POST /api/auth/login, then press Authorize and paste the token."
            });

            // Descriptions and examples come from the XML comments in the code.
            var xmlFile = $"{typeof(SwaggerExtensions).Assembly.GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), includeControllerXmlComments: true);

            // Authorize button: the user pastes the JWT, Swagger sends it as "Authorization: Bearer <token>".
            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT from POST /api/auth/login. Paste only the token, without the word Bearer."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerScheme, document)] = []
            });
        });
        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket System API v1");
            options.DocumentTitle = "Ticket System API";
        });
        return app;
    }
}
