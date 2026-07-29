using System.Linq.Expressions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.TestingLab;

public sealed class TestingEventHandlers(IApplicationDbContext context, IActorContextAccessor actorContextAccessor) :
    ICommandHandler<CreateTestingEventCommand, Result<TestingEventProjection>>,
    ICommandHandler<UpdateTestingEventCommand, Result<TestingEventProjection>>,
    ICommandHandler<DeleteTestingEventCommand, Result<bool>>,
    ICommandHandler<OpenTestingEventApplicationsCommand, Result<TestingEventProjection>>,
    ICommandHandler<CloseTestingEventApplicationsCommand, Result<TestingEventProjection>>,
    ICommandHandler<CreateTestingEventSlotCommand, Result<TestingEventSlotProjection>>,
    ICommandHandler<UpdateTestingEventSlotCommand, Result<TestingEventSlotProjection>>,
    ICommandHandler<DeleteTestingEventSlotCommand, Result<bool>>,
    IQueryHandler<GetTestingEventQuery, Result<TestingEventProjection>>,
    IQueryHandler<GetTestingEventsQuery, Result<IReadOnlyList<TestingEventProjection>>>,
    IQueryHandler<GetTestingEventSlotsQuery, Result<IReadOnlyList<TestingEventSlotProjection>>>
{
    public async Task<Result<TestingEventProjection>> Handle(CreateTestingEventCommand request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return Result.Failure<TestingEventProjection>(actor.Error);

        try
        {
            var testingEvent = TestingEvent.Create(
                request.Name,
                request.Mode,
                actor.UserId,
                request.ApplicationsOpenAt,
                request.ApplicationsCloseAt,
                request.StartsAt,
                request.EndsAt,
                request.RequiresFeedback,
                request.ApprovalMode,
                actor.TenantId,
                request.Description);
            context.Set<TestingEvent>().Add(testingEvent);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(testingEvent));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<TestingEventProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<TestingEventProjection>> Handle(UpdateTestingEventCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<TestingEventProjection>(authorization.Error);

        try
        {
            authorization.Event!.Update(
                request.Name,
                request.Description,
                request.Mode,
                request.ApprovalMode,
                request.ApplicationsOpenAt,
                request.ApplicationsCloseAt,
                request.StartsAt,
                request.EndsAt,
                request.RequiresFeedback);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(authorization.Event));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<TestingEventProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<bool>> Handle(DeleteTestingEventCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<bool>(authorization.Error);
        var testingEvent = authorization.Event!;
        if (testingEvent.Status != TestingEventStatus.Draft)
            return Result.Failure<bool>(Validation("Only draft events can be deleted."));
        var hasApplications = await context.Set<TestingProjectApplication>()
            .AnyAsync(application => application.EventId == testingEvent.Id && application.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (hasApplications)
            return Result.Failure<bool>(Error.Conflict("TestingLab.EventHasApplications", "Events with applications cannot be deleted."));

        testingEvent.DeletedAt = SystemClock.UtcNow;
        testingEvent.Touch();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(true);
    }

    public async Task<Result<TestingEventProjection>> Handle(OpenTestingEventApplicationsCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<TestingEventProjection>(authorization.Error);
        try
        {
            authorization.Event!.OpenApplications();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(authorization.Event));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<TestingEventProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<TestingEventProjection>> Handle(CloseTestingEventApplicationsCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<TestingEventProjection>(authorization.Error);
        try
        {
            authorization.Event!.CloseApplications();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(authorization.Event));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<TestingEventProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<TestingEventSlotProjection>> Handle(CreateTestingEventSlotCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<TestingEventSlotProjection>(authorization.Error);
        if (!IsWithinEvent(authorization.Event!, request.StartsAt, request.EndsAt))
            return Result.Failure<TestingEventSlotProjection>(Validation("Slot schedule must be inside the event schedule."));

        try
        {
            var slot = TestingEventSlot.Create(
                request.EventId,
                request.Mode,
                request.StartsAt,
                request.EndsAt,
                request.MaxTesters,
                request.MaxProjects,
                request.CampusName,
                request.RoomName,
                request.MeetingUrl,
                authorization.Event!.TenantId,
                request.LocationId);
            context.Set<TestingEventSlot>().Add(slot);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(slot));
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<TestingEventSlotProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<TestingEventSlotProjection>> Handle(UpdateTestingEventSlotCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<TestingEventSlotProjection>(authorization.Error);
        if (!IsWithinEvent(authorization.Event!, request.StartsAt, request.EndsAt))
            return Result.Failure<TestingEventSlotProjection>(Validation("Slot schedule must be inside the event schedule."));
        var slot = await context.Set<TestingEventSlot>()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.SlotId &&
                candidate.EventId == request.EventId &&
                candidate.TenantId == authorization.Event!.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (slot == null)
            return Result.Failure<TestingEventSlotProjection>(Error.NotFound("TestingLab.EventSlotNotFound", "Testing event slot not found."));

        try
        {
            slot.Update(
                request.Mode,
                request.StartsAt,
                request.EndsAt,
                request.MaxTesters,
                request.MaxProjects,
                request.CampusName,
                request.RoomName,
                request.MeetingUrl,
                request.LocationId);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(slot));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<TestingEventSlotProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<bool>> Handle(DeleteTestingEventSlotCommand request, CancellationToken cancellationToken)
    {
        var authorization = await GetManagedEventAsync(request.EventId, cancellationToken).ConfigureAwait(false);
        if (authorization.Error != null) return Result.Failure<bool>(authorization.Error);
        var slot = await context.Set<TestingEventSlot>()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.SlotId &&
                candidate.EventId == request.EventId &&
                candidate.TenantId == authorization.Event!.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (slot == null)
            return Result.Failure<bool>(Error.NotFound("TestingLab.EventSlotNotFound", "Testing event slot not found."));
        var assigned = await context.Set<TestingProjectApplication>()
            .AnyAsync(application =>
                application.AssignedSlotId == slot.Id &&
                application.Status == TestingApplicationStatus.Approved &&
                application.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (assigned)
            return Result.Failure<bool>(Error.Conflict("TestingLab.EventSlotAssigned", "A slot with approved projects cannot be deleted."));

        slot.DeletedAt = SystemClock.UtcNow;
        slot.Touch();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(true);
    }

    public async Task<Result<TestingEventProjection>> Handle(GetTestingEventQuery request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return Result.Failure<TestingEventProjection>(actor.Error);
        var projection = await context.Set<TestingEvent>()
            .AsNoTracking()
            .Where(testingEvent =>
                testingEvent.Id == request.EventId &&
                testingEvent.TenantId == actor.TenantId &&
                testingEvent.DeletedAt == null)
            .Select(EventProjection)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return projection == null
            ? Result.Failure<TestingEventProjection>(Error.NotFound("TestingLab.EventNotFound", "Testing event not found."))
            : Result.Success(projection);
    }

    public async Task<Result<IReadOnlyList<TestingEventProjection>>> Handle(GetTestingEventsQuery request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return Result.Failure<IReadOnlyList<TestingEventProjection>>(actor.Error);
        var query = context.Set<TestingEvent>()
            .AsNoTracking()
            .Where(testingEvent => testingEvent.TenantId == actor.TenantId && testingEvent.DeletedAt == null);
        if (request.Status.HasValue) query = query.Where(testingEvent => testingEvent.Status == request.Status.Value);
        var events = await query
            .OrderByDescending(testingEvent => testingEvent.StartsAt)
            .Skip(Math.Max(0, request.Skip))
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(EventProjection)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<TestingEventProjection>>(events);
    }

    public async Task<Result<IReadOnlyList<TestingEventSlotProjection>>> Handle(GetTestingEventSlotsQuery request, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return Result.Failure<IReadOnlyList<TestingEventSlotProjection>>(actor.Error);
        var eventExists = await context.Set<TestingEvent>().AnyAsync(testingEvent =>
            testingEvent.Id == request.EventId &&
            testingEvent.TenantId == actor.TenantId &&
            testingEvent.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (!eventExists)
            return Result.Failure<IReadOnlyList<TestingEventSlotProjection>>(Error.NotFound("TestingLab.EventNotFound", "Testing event not found."));

        var slots = await context.Set<TestingEventSlot>()
            .AsNoTracking()
            .Where(slot => slot.EventId == request.EventId && slot.TenantId == actor.TenantId && slot.DeletedAt == null)
            .OrderBy(slot => slot.StartsAt)
            .Select(slot => new TestingEventSlotProjection(
                slot.Id,
                slot.EventId,
                slot.LocationId,
                slot.Mode,
                slot.StartsAt,
                slot.EndsAt,
                slot.MaxTesters,
                slot.MaxProjects,
                slot.CampusName,
                slot.RoomName,
                slot.MeetingUrl,
                context.Set<TestingProjectApplication>().Count(application =>
                    application.AssignedSlotId == slot.Id &&
                    application.Status == TestingApplicationStatus.Approved &&
                    application.DeletedAt == null),
                context.Set<TestingSession>()
                    .Where(session => session.EventSlotId == slot.Id && session.DeletedAt == null)
                    .SelectMany(session => session.Registrations)
                    .Count(registration => registration.DeletedAt == null)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<TestingEventSlotProjection>>(slots);
    }

    private async Task<ManagedEvent> GetManagedEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return new(null, actor.Error);
        var testingEvent = await context.Set<TestingEvent>()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == eventId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (testingEvent == null)
            return new(null, Error.NotFound("TestingLab.EventNotFound", "Testing event not found."));
        if (testingEvent.ManagerUserId != actor.UserId)
            return new(null, Error.Forbidden("TestingLab.EventManagerRequired", "Only the event manager can perform this operation."));
        return new(testingEvent, null);
    }

    private async Task<ActorScope> RequireActorAsync(CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        var userId = actor.SubjectIdAsGuid;
        if (!actor.IsAuthenticated || userId == null || actor.TenantId == null)
            return new(Guid.Empty, Guid.Empty, Error.Unauthorized("TestingLab.Unauthenticated", "An authenticated tenant actor is required."));
        var activeUser = await context.Set<User>().AsNoTracking().AnyAsync(user =>
            user.Id == userId.Value && user.IsActive && !user.IsSuspended && user.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        var activeMembership = activeUser && await context.Set<TenantMember>().AsNoTracking().AnyAsync(member =>
            member.UserId == userId.Value &&
            member.TenantId == actor.TenantId.Value &&
            member.IsActive &&
            member.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        return activeMembership
            ? new(userId.Value, actor.TenantId.Value, null)
            : new(Guid.Empty, Guid.Empty, Error.Unauthorized("TestingLab.InactiveActor", "An active user and tenant membership are required."));
    }

    private static bool IsWithinEvent(TestingEvent testingEvent, DateTime startsAt, DateTime endsAt) =>
        startsAt >= testingEvent.StartsAt && endsAt <= testingEvent.EndsAt;

    private static Error Validation(string message) => Error.Validation("TestingLab.Validation", message);

    private static TestingEventProjection ToProjection(TestingEvent testingEvent) => new(
        testingEvent.Id,
        testingEvent.Name,
        testingEvent.Description,
        testingEvent.Mode,
        testingEvent.ApprovalMode,
        testingEvent.Status,
        testingEvent.ManagerUserId,
        testingEvent.ApplicationsOpenAt,
        testingEvent.ApplicationsCloseAt,
        testingEvent.StartsAt,
        testingEvent.EndsAt,
        testingEvent.RequiresFeedback,
        testingEvent.LearningCompletionRequirement,
        testingEvent.CourseId,
        testingEvent.CohortId,
        testingEvent.LearningActivityId,
        testingEvent.TenantId,
        testingEvent.Slots.Count(slot => slot.DeletedAt == null),
        testingEvent.Applications.Count(application => application.DeletedAt == null));

    private static readonly Expression<Func<TestingEvent, TestingEventProjection>> EventProjection = testingEvent => new(
        testingEvent.Id,
        testingEvent.Name,
        testingEvent.Description,
        testingEvent.Mode,
        testingEvent.ApprovalMode,
        testingEvent.Status,
        testingEvent.ManagerUserId,
        testingEvent.ApplicationsOpenAt,
        testingEvent.ApplicationsCloseAt,
        testingEvent.StartsAt,
        testingEvent.EndsAt,
        testingEvent.RequiresFeedback,
        testingEvent.LearningCompletionRequirement,
        testingEvent.CourseId,
        testingEvent.CohortId,
        testingEvent.LearningActivityId,
        testingEvent.TenantId,
        testingEvent.Slots.Count(slot => slot.DeletedAt == null),
        testingEvent.Applications.Count(application => application.DeletedAt == null));
    private static TestingEventSlotProjection ToProjection(TestingEventSlot slot) => new(
        slot.Id,
        slot.EventId,
        slot.LocationId,
        slot.Mode,
        slot.StartsAt,
        slot.EndsAt,
        slot.MaxTesters,
        slot.MaxProjects,
        slot.CampusName,
        slot.RoomName,
        slot.MeetingUrl,
        0,
        0);

    private sealed record ActorScope(Guid UserId, Guid TenantId, Error? Error);
    private sealed record ManagedEvent(TestingEvent? Event, Error? Error);
}