namespace TicketSystem.Api.Infrastructure;

/// <summary>
/// Base class for errors that the client caused and that have a fitting HTTP status.
/// The global handler turns them into ProblemDetails responses.
/// </summary>
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>The requested item does not exist (404).</summary>
public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);

/// <summary>The request breaks a business rule, for example a wrong status change (409).</summary>
public class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);

/// <summary>The user is signed in but is not allowed to do this (403).</summary>
public class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);

/// <summary>The request data is wrong in a way that attribute validation cannot detect (400).</summary>
public class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);
