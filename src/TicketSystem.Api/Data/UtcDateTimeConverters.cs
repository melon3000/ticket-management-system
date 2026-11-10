using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TicketSystem.Api.Data;

/// <summary>
/// SQLite does not remember that a stored time is UTC, so EF Core reads it back with an unspecified kind and the JSON
/// output loses its "Z". This converter marks every time read from the database as UTC.
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

/// <summary>Same as <see cref="UtcDateTimeConverter"/> for optional times such as ResolvedAt.</summary>
public class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    v => v.HasValue && v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v,
    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
