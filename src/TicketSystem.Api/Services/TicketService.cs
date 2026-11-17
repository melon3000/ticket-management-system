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

    public async Task<PagedResult<TicketDto>> ListAsync(TicketQuery query, int userId, UserRole role)
    {
        var filter = new TicketFilter(
            query.Status, query.Priority, query.Assignee, query.CategoryId, query.Search,
            role == UserRole.User ? userId : null,
            query.SortBy, query.Descending, query.Page, query.PageSize);

        var (items, totalCount) = await tickets.SearchAsync(filter);
        return new PagedResult<TicketDto>(items.Select(t => t.ToDto()).ToList(), query.Page, query.PageSize, totalCount);
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

    public async Task<TicketDto> AssignAsync(int id, int userId)
    {
        var ticket = await GetExistingAsync(id);
        if (ticket.Status != TicketStatus.New || ticket.AssigneeId is not null)
        {
            throw new ConflictException(ticket.Status == TicketStatus.Resolved
                ? "A resolved ticket cannot be taken into work."
                : "The ticket has already been taken into work.");
        }

        var supporter = await users.GetByIdAsync(userId)
                        ?? throw new ForbiddenException("The signed-in user no longer exists.");
        var now = clock.GetUtcNow().UtcDateTime;

        ticket.Assignee = supporter;
        ticket.Status = TicketStatus.InProgress;
        await logs.AddAsync(new TicketLog
        {
            Ticket = ticket, UserId = userId, Action = TicketLogActions.Assigned,
            NewValue = supporter.DisplayName, CreatedAt = now
        });
        await logs.AddAsync(new TicketLog
        {
            Ticket = ticket, UserId = userId, Action = TicketLogActions.StatusChanged,
            OldValue = nameof(TicketStatus.New), NewValue = nameof(TicketStatus.InProgress), CreatedAt = now
        });
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Ticket {TicketId} taken into work by user {UserId}", id, userId);
        return ticket.ToDto();
    }

    public async Task<TicketDto> ChangeStatusAsync(int id, TicketStatus status, int userId, UserRole role)
    {
        var ticket = await GetExistingAsync(id);
        if (role != UserRole.Admin && ticket.AssigneeId != userId)
        {
            throw new ForbiddenException("Only the assigned support person or an administrator can change the status.");
        }

        var allowed = (ticket.Status, status) is
            (TicketStatus.InProgress, TicketStatus.Resolved) or (TicketStatus.Resolved, TicketStatus.InProgress);
        if (!allowed)
        {
            throw new ConflictException(ticket.Status == TicketStatus.New
                ? "A new ticket must be taken into work first."
                : $"A ticket cannot change from {ticket.Status} to {status}.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.ResolvedAt = status == TicketStatus.Resolved ? now : null;
        await logs.AddAsync(new TicketLog
        {
            Ticket = ticket, UserId = userId, Action = TicketLogActions.StatusChanged,
            OldValue = oldStatus.ToString(), NewValue = status.ToString(), CreatedAt = now
        });
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Ticket {TicketId} changed from {OldStatus} to {NewStatus} by user {UserId}",
            id, oldStatus, status, userId);
        return ticket.ToDto();
    }

    public async Task<List<TicketLogDto>> GetLogAsync(int id, int userId, UserRole role)
    {
        var ticket = await GetExistingAsync(id);
        if (role == UserRole.User && ticket.CreatedById != userId)
        {
            throw new ForbiddenException("You can only view the log of your own tickets.");
        }

        var entries = await logs.GetByTicketIdAsync(id);
        return entries.Select(l => l.ToDto()).ToList();
    }

    public async Task<List<TicketDto>> GetOverdueAsync()
    {
        var overdue = await tickets.GetOverdueAsync(clock.GetUtcNow().UtcDateTime);
        return overdue.Select(t => t.ToDto()).ToList();
    }

    public async Task<TicketDto> EscalateAsync(int id, int userId)
    {
        var ticket = await GetExistingAsync(id);
        var now = clock.GetUtcNow().UtcDateTime;

        if (ticket.Status == TicketStatus.Resolved)
        {
            throw new ConflictException("A resolved ticket cannot be escalated.");
        }

        if (ticket.IsOverdue)
        {
            throw new ConflictException("The ticket has already been escalated.");
        }

        if (ticket.Deadline >= now)
        {
            throw new ConflictException("The deadline of the ticket has not passed yet.");
        }

        var oldPriority = ticket.Priority;
        var higher = await priorities.GetNextHigherAsync(oldPriority.Level);
        if (higher is not null)
        {
            ticket.Priority = higher;
        }

        ticket.IsOverdue = true;
        await logs.AddAsync(new TicketLog
        {
            Ticket = ticket, UserId = userId, Action = TicketLogActions.Escalated,
            OldValue = oldPriority.Name, NewValue = ticket.Priority.Name, CreatedAt = now
        });
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Ticket {TicketId} escalated from {OldPriority} to {NewPriority}",
            id, oldPriority.Name, ticket.Priority.Name);
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
