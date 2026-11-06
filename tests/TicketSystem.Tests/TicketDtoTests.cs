using System.ComponentModel.DataAnnotations;
using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Mapping;

namespace TicketSystem.Tests;

public class TicketDtoTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static CreateTicketRequest ValidRequest() => new()
    {
        Title = "Printer ei tööta",
        Description = "Kolmanda korruse printer ei prindi.",
        CategoryId = 1,
        PriorityId = 2
    };

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        Assert.Empty(Validate(ValidRequest()));
    }

    [Theory]
    [InlineData("abc")]                      // shorter than 5 characters
    [InlineData("")]                         // empty
    public void TooShortOrEmptyTitle_FailsValidation(string title)
    {
        var request = ValidRequest();
        request.Title = title;

        Assert.Contains(Validate(request), r => r.MemberNames.Contains(nameof(CreateTicketRequest.Title)));
    }

    [Fact]
    public void TooLongDescriptionAndMissingCategory_FailValidation()
    {
        var request = ValidRequest();
        request.Description = new string('x', 2001);
        request.CategoryId = 0;

        var members = Validate(request).SelectMany(r => r.MemberNames).ToList();

        Assert.Contains(nameof(CreateTicketRequest.Description), members);
        Assert.Contains(nameof(CreateTicketRequest.CategoryId), members);
    }

    [Fact]
    public void ToDto_CopiesFieldsAndHandlesMissingAssignee()
    {
        var ticket = new Ticket
        {
            Id = 7,
            Title = "Võrk on maas",
            Description = "Ruumis 204 puudub internet.",
            Status = TicketStatus.New,
            Category = new Category { Id = 3, Name = "Võrk" },
            Priority = new Priority { Id = 2, Name = "Keskmine", Level = 2, DeadlineHours = 24 },
            CreatedBy = new User { Id = 5, DisplayName = "Kati Kask", Email = "kati@example.com" },
            CreatedAt = new DateTime(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc),
            Deadline = new DateTime(2026, 11, 7, 8, 0, 0, DateTimeKind.Utc)
        };

        var dto = ticket.ToDto();

        Assert.Equal(7, dto.Id);
        Assert.Equal("Võrk", dto.Category.Name);
        Assert.Equal(24, dto.Priority.DeadlineHours);
        Assert.Equal("Kati Kask", dto.CreatedBy.DisplayName);
        Assert.Null(dto.Assignee);
        Assert.Null(dto.ResolvedAt);
    }
}
