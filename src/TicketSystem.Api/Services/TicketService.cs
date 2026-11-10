using TicketSystem.Api.Data.Entities;
using TicketSystem.Api.Dtos;
using TicketSystem.Api.Infrastructure;
using TicketSystem.Api.Mapping;
using TicketSystem.Api.Repositories;

namespace TicketSystem.Api.Services;

public class TicketService(
    ITicketRepository tickets,
    ICategoryRepository categories,
    IPriorityRepository priorities,
    IUserRepository users,
    ITicketLogRepository logs,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<TicketService> logger) : ITicketService
{
    public async Task<TicketDto> CreateAsync(CreateTicketRequest request, int userId)
    {
        var author = await users.GetByIdAsync(userId)
                     ?? throw new ForbiddenException("The signed-in user no longer exists.");
        var category = await categories.GetByIdAsync(request.CategoryId)
                       ?? throw new BadRequestException($"Category {request.CategoryId} does not exist.");
        var priority = await priorities.GetByIdAsync(request.PriorityId)
                       ?? throw new BadRequestException($"Priority {request.PriorityId} does not exist.");

        var now = clock.GetUtcNow().UtcDateTime;
        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = TicketStatus.New,
            Category = category,
            Priority = priority,
            CreatedBy = author,
            CreatedAt = now,
            Deadline = now.AddHours(priority.DeadlineHours)
        };

        await tickets.AddAsync(ticket);
        await logs.AddAsync(new TicketLog
        {
            Ticket = ticket,
            UserId = userId,
            Action = TicketLogActions.Created,
            NewValue = TicketStatus.New.ToString(),
            CreatedAt = now
        });
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Ticket {TicketId} created by user {UserId} with priority {Priority}",
            ticket.Id, userId, priority.Name);
        return ticket.ToDto();
    }

    public async Task<TicketDto> GetAsync(int id, int userId, UserRole role)
    {
        var ticket = await GetExistingAsync(id);
        if (role == UserRole.User && ticket.CreatedById != userId)
        {
            throw new ForbiddenException("You can only view your own tickets.");
        }

        return ticket.ToDto();
    }

    public async Task<TicketDto> UpdateAsync(int id, UpdateTicketRequest request, int userId, UserRole role)
    {
        var ticket = await GetExistingAsync(id);

        if (ticket.Status == TicketStatus.Resolved)
        {
            throw new ConflictException("A resolved ticket cannot be edited.");
        }

        var isAdmin = role == UserRole.Admin;
        var isAuthorOfNewTicket = ticket.CreatedById == userId && ticket.Status == TicketStatus.New;
        if (!isAdmin && !isAuthorOfNewTicket)
        {
            throw new ForbiddenException("Only the author of a new ticket or an administrator can edit it.");
        }

        if (request.CategoryId != ticket.CategoryId)
        {
            ticket.Category = await categories.GetByIdAsync(request.CategoryId)
                              ?? throw new BadRequestException($"Category {request.CategoryId} does not exist.");
        }

        if (request.PriorityId != ticket.PriorityId)
        {
            var priority = await priorities.GetByIdAsync(request.PriorityId)
                           ?? throw new BadRequestException($"Priority {request.PriorityId} does not exist.");
            ticket.Priority = priority;
            ticket.Deadline = ticket.CreatedAt.AddHours(priority.DeadlineHours);
        }

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Ticket {TicketId} edited by user {UserId}", id, userId);
        return ticket.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var ticket = await GetExistingAsync(id);
        tickets.Remove(ticket);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Ticket {TicketId} deleted", id);
    }

    private async Task<Ticket> GetExistingAsync(int id) =>
        await tickets.GetByIdAsync(id) ?? throw new NotFoundException($"Ticket {id} was not found.");
}
