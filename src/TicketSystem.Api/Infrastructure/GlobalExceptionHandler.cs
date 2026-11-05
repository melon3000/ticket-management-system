using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Api.Infrastructure;

/// <summary>
/// Catches every exception that a controller or service does not handle and answers with ProblemDetails.
/// Expected errors (<see cref="AppException"/>) keep their message, unexpected errors get a generic text
/// so that no internal details leak to the client. Both are logged.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is AppException appException)
        {
            logger.LogWarning("Request {Method} {Path} failed with {Status}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, appException.StatusCode, appException.Message);

            problem = new ProblemDetails
            {
                Status = appException.StatusCode,
                Title = Titles.GetValueOrDefault(appException.StatusCode, "Request failed"),
                Detail = appException.Message
            };
        }
        else
        {
            logger.LogError(exception, "Unhandled exception in {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = "Something went wrong on the server. Please try again later."
            };
        }

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }

    private static readonly Dictionary<int, string> Titles = new()
    {
        [StatusCodes.Status400BadRequest] = "Bad request",
        [StatusCodes.Status403Forbidden] = "Forbidden",
        [StatusCodes.Status404NotFound] = "Not found",
        [StatusCodes.Status409Conflict] = "Conflict"
    };
}
