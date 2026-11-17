using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Api.Dtos;

/// <summary>Data for adding a comment to a ticket.</summary>
public class CreateCommentRequest
{
    /// <example>Tere! Proovisin printerit taaskäivitada, aga viga jäi.</example>
    [Required, StringLength(1000, MinimumLength = 2)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>A comment as shown to API clients.</summary>
/// <param name="Id">Comment id.</param>
/// <param name="Text">The message.</param>
/// <param name="Author">Who wrote it.</param>
/// <param name="CreatedAt">When it was written (UTC).</param>
public record CommentDto(int Id, string Text, UserSummaryDto Author, DateTime CreatedAt);
