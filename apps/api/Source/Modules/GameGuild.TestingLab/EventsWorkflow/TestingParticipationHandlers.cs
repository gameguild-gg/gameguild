using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.Extensions.Logging;

namespace GameGuild.TestingLab;

public sealed class TestingParticipationHandlers(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,
    ILogger<TestingParticipationHandlers> logger,
    IProjectLifecycleLock? capacityLock = null,
    IPublisher? publisher = null) :
    ICommandHandler<RegisterTestingEventSlotCommand, Result<TestingSlotRegistrationProjection>>,
    ICommandHandler<CancelTestingEventSlotRegistrationCommand, Result<TestingSlotRegistrationProjection>>,
    ICommandHandler<CheckInTestingEventRegistrationCommand, Result<TestingSlotRegistrationProjection>>,
    ICommandHandler<CheckOutTestingEventRegistrationCommand, Result<TestingSlotRegistrationProjection>>,
    ICommandHandler<MarkTestingEventNoShowCommand, Result<TestingSlotRegistrationProjection>>,
    ICommandHandler<AssignTestingProjectToTesterCommand, Result<TestingFeedbackObligationProjection>>,
    ICommandHandler<SubmitTestingEventFeedbackCommand, Result<TestingEventFeedbackProjection>>,
    ICommandHandler<CompleteTestingEventParticipationCommand, Result<TestingSlotRegistrationProjection>>,
    IQueryHandler<GetMyTestingSlotRegistrationsQuery, Result<IReadOnlyList<TestingSlotRegistrationProjection>>>,
    IQueryHandler<GetTestingEventSlotRegistrationsQuery, Result<IReadOnlyList<TestingSlotRegistrationProjection>>>,
    IQueryHandler<GetMyTestingFeedbackObligationsQuery, Result<IReadOnlyList<TestingFeedbackObligationProjection>>>,
    IQueryHandler<GetTestingEventFeedbackQuery, Result<IReadOnlyList<TestingEventFeedbackReviewProjection>>>
{
    private static readonly TestingSlotRegistrationStatus[] CapacityStatuses =
    [
        TestingSlotRegistrationStatus.Registered,
        TestingSlotRegistrationStatus.CheckedIn,
        TestingSlotRegistrationStatus.Attended,
        TestingSlotRegistrationStatus.Completed,
        TestingSlotRegistrationStatus.NoShow,
    ];

    private readonly IProjectLifecycleLock _capacityLock = capacityLock ?? new ProjectLifecycleLock(context);

    public async Task<Result<TestingSlotRegistrationProjection>> Handle(
        RegisterTestingEventSlotCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return Result.Failure<TestingSlotRegistrationProjection>(actor.Error);

        await using var lockHandle = await _capacityLock.AcquireAsync(request.SlotId, cancellationToken).ConfigureAwait(false);
        var slot = await context.Set<TestingEventSlot>()
            .Include(candidate => candidate.Event)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.SlotId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (slot == null)
            return Result.Failure<TestingSlotRegistrationProjection>(
                Error.NotFound("TestingLab.EventSlotNotFound", "Testing event slot not found."));
        if (slot.Event.Status is not (
                TestingEventStatus.ApplicationsClosed or
                TestingEventStatus.Scheduled or
                TestingEventStatus.Active))
            return Result.Failure<TestingSlotRegistrationProjection>(
                Validation("Tester registration is not open for this event."));

        var existing = await context.Set<TestingSlotRegistration>()
            .FirstOrDefaultAsync(candidate =>
                candidate.SlotId == slot.Id &&
                candidate.UserId == actor.UserId &&
                candidate.Status != TestingSlotRegistrationStatus.Cancelled &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing != null)
            return Result.Success(await ToProjectionAsync(existing, cancellationToken).ConfigureAwait(false));

        var capacityCount = await context.Set<TestingSlotRegistration>().CountAsync(candidate =>
            candidate.SlotId == slot.Id &&
            CapacityStatuses.Contains(candidate.Status) &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        TestingSlotRegistration registration;
        if (slot.MaxTesters.HasValue && capacityCount >= slot.MaxTesters.Value)
        {
            var nextPosition = (await context.Set<TestingSlotRegistration>()
                .Where(candidate =>
                    candidate.SlotId == slot.Id &&
                    candidate.Status == TestingSlotRegistrationStatus.Waitlisted &&
                    candidate.DeletedAt == null)
                .MaxAsync(candidate => (int?)candidate.WaitlistPosition, cancellationToken)
                .ConfigureAwait(false) ?? 0) + 1;
            registration = TestingSlotRegistration.Waitlist(
                slot.EventId,
                slot.Id,
                actor.UserId,
                nextPosition,
                request.Notes,
                actor.TenantId);
        }
        else
        {
            registration = TestingSlotRegistration.Register(
                slot.EventId,
                slot.Id,
                actor.UserId,
                request.Notes,
                actor.TenantId);
        }

        context.Set<TestingSlotRegistration>().Add(registration);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await lockHandle.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(await ToProjectionAsync(registration, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<TestingSlotRegistrationProjection>> Handle(
        CancelTestingEventSlotRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadRegistrationAsync(request.RegistrationId, cancellationToken).ConfigureAwait(false);
        if (loaded.Error != null) return Result.Failure<TestingSlotRegistrationProjection>(loaded.Error);
        var managerOverride = loaded.Registration!.Event.ManagerUserId == loaded.Actor!.UserId;
        if (!managerOverride && loaded.Registration.UserId != loaded.Actor.UserId)
            return Result.Failure<TestingSlotRegistrationProjection>(
                Error.Forbidden("TestingLab.RegistrationOwnerRequired", "Only the tester or event manager can cancel this registration."));

        await using var lockHandle = await _capacityLock.AcquireAsync(loaded.Registration.SlotId, cancellationToken).ConfigureAwait(false);
        var releasedCapacity = loaded.Registration.ConsumesCapacity;
        try
        {
            loaded.Registration.Cancel(loaded.Actor.UserId, managerOverride);
            if (releasedCapacity)
                await PromoteWaitlistAsync(loaded.Registration.SlotId, loaded.Actor.TenantId, cancellationToken).ConfigureAwait(false);
            else
                await ReindexWaitlistAsync(loaded.Registration.SlotId, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await lockHandle.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(await ToProjectionAsync(loaded.Registration, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is InvalidOperationException or UnauthorizedAccessException)
        {
            return Result.Failure<TestingSlotRegistrationProjection>(Validation(exception.Message));
        }
    }

    public Task<Result<TestingSlotRegistrationProjection>> Handle(
        CheckInTestingEventRegistrationCommand request,
        CancellationToken cancellationToken)
        => ChangeAttendanceAsync(request.RegistrationId, static registration => registration.CheckIn(), cancellationToken);

    public Task<Result<TestingSlotRegistrationProjection>> Handle(
        CheckOutTestingEventRegistrationCommand request,
        CancellationToken cancellationToken)
        => ChangeAttendanceAsync(request.RegistrationId, static registration => registration.CheckOut(), cancellationToken);

    public Task<Result<TestingSlotRegistrationProjection>> Handle(
        MarkTestingEventNoShowCommand request,
        CancellationToken cancellationToken)
        => ChangeAttendanceAsync(request.RegistrationId, static registration => registration.MarkNoShow(), cancellationToken);

    public async Task<Result<TestingFeedbackObligationProjection>> Handle(
        AssignTestingProjectToTesterCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadManagedRegistrationAsync(request.RegistrationId, cancellationToken).ConfigureAwait(false);
        if (loaded.Error != null) return Result.Failure<TestingFeedbackObligationProjection>(loaded.Error);
        if (loaded.Registration!.Status is not (
                TestingSlotRegistrationStatus.CheckedIn or
                TestingSlotRegistrationStatus.Attended))
            return Result.Failure<TestingFeedbackObligationProjection>(
                Validation("The tester must attend the slot before a tested project can be assigned."));

        var application = await context.Set<TestingProjectApplication>()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.ApplicationId &&
                candidate.AssignedSlotId == loaded.Registration.SlotId &&
                candidate.Status == TestingApplicationStatus.Approved &&
                candidate.TenantId == loaded.Actor!.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (application == null)
            return Result.Failure<TestingFeedbackObligationProjection>(
                Error.NotFound("TestingLab.ApprovedApplicationNotFound", "An approved project application was not found in this slot."));

        var existing = await context.Set<TestingFeedbackObligation>()
            .FirstOrDefaultAsync(candidate =>
                candidate.SlotId == loaded.Registration.SlotId &&
                candidate.ApplicationId == application.Id &&
                candidate.TesterUserId == loaded.Registration.UserId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing != null) return Result.Success(ToProjection(existing));

        var obligation = TestingFeedbackObligation.Create(
            loaded.Registration.EventId,
            loaded.Registration.SlotId,
            application.Id,
            loaded.Registration.UserId,
            loaded.Actor!.TenantId);
        if (!loaded.Registration.Event.RequiresFeedback) obligation.Waive();
        context.Set<TestingFeedbackObligation>().Add(obligation);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(ToProjection(obligation));
    }

    public async Task<Result<TestingEventFeedbackProjection>> Handle(
        SubmitTestingEventFeedbackCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return Result.Failure<TestingEventFeedbackProjection>(actor.Error);
        var obligation = await context.Set<TestingFeedbackObligation>()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.ObligationId &&
                candidate.TesterUserId == actor.UserId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (obligation == null)
            return Result.Failure<TestingEventFeedbackProjection>(
                Error.NotFound("TestingLab.FeedbackObligationNotFound", "Feedback obligation not found."));
        if (obligation.IsFulfilled)
            return Result.Failure<TestingEventFeedbackProjection>(Validation("The feedback obligation is already complete."));

        var registration = await context.Set<TestingSlotRegistration>()
            .FirstOrDefaultAsync(candidate =>
                candidate.SlotId == obligation.SlotId &&
                candidate.UserId == actor.UserId &&
                candidate.Status != TestingSlotRegistrationStatus.Cancelled &&
                candidate.Status != TestingSlotRegistrationStatus.NoShow &&
                candidate.Status != TestingSlotRegistrationStatus.Waitlisted &&
                candidate.Status != TestingSlotRegistrationStatus.Registered &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (registration == null)
            return Result.Failure<TestingEventFeedbackProjection>(
                Error.Forbidden("TestingLab.AttendanceRequired", "Attendance is required before feedback can be submitted."));
        var slotMode = await context.Set<TestingEventSlot>()
            .Where(candidate => candidate.Id == obligation.SlotId)
            .Select(candidate => candidate.Mode)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var feedback = TestingFeedback.CreateForEvent(
                obligation.EventId,
                obligation.ApplicationId,
                actor.UserId,
                slotMode == TestingEventMode.InPerson ? TestingContext.InPerson : TestingContext.Online,
                request.FeedbackData,
                request.OverallRating,
                request.WouldRecommend,
                request.AdditionalNotes,
                actor.TenantId);
            context.Set<TestingFeedback>().Add(feedback);
            obligation.Fulfill(feedback.Id);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(ToProjection(feedback));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<TestingEventFeedbackProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<TestingSlotRegistrationProjection>> Handle(
        CompleteTestingEventParticipationCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadRegistrationAsync(request.RegistrationId, cancellationToken).ConfigureAwait(false);
        if (loaded.Error != null) return Result.Failure<TestingSlotRegistrationProjection>(loaded.Error);
        if (loaded.Registration!.UserId != loaded.Actor!.UserId &&
            loaded.Registration.Event.ManagerUserId != loaded.Actor.UserId)
            return Result.Failure<TestingSlotRegistrationProjection>(
                Error.Forbidden("TestingLab.RegistrationOwnerRequired", "Only the tester or event manager can complete participation."));
        var pending = await context.Set<TestingFeedbackObligation>().AnyAsync(candidate =>
            candidate.SlotId == loaded.Registration.SlotId &&
            candidate.TesterUserId == loaded.Registration.UserId &&
            candidate.Status == TestingFeedbackObligationStatus.Pending &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        if (pending)
            return Result.Failure<TestingSlotRegistrationProjection>(
                Validation("All required feedback must be submitted before participation can be completed."));
        try
        {
            if (loaded.Registration.Status != TestingSlotRegistrationStatus.Completed)
            {
                loaded.Registration.Complete();
            }

            var evidence = await CreateLearningEvidenceAsync(loaded.Registration, cancellationToken)
                .ConfigureAwait(false);
            if (evidence != null && publisher != null)
            {
                await publisher.Publish(evidence, cancellationToken).ConfigureAwait(false);
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(await ToProjectionAsync(loaded.Registration, cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<TestingSlotRegistrationProjection>(Validation(exception.Message));
        }
    }

    public async Task<Result<IReadOnlyList<TestingSlotRegistrationProjection>>> Handle(
        GetTestingEventSlotRegistrationsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null)
            return Result.Failure<IReadOnlyList<TestingSlotRegistrationProjection>>(actor.Error);
        var slot = await context.Set<TestingEventSlot>()
            .Include(candidate => candidate.Event)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.SlotId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (slot == null)
            return Result.Failure<IReadOnlyList<TestingSlotRegistrationProjection>>(
                Error.NotFound("TestingLab.EventSlotNotFound", "Testing event slot not found."));
        if (slot.Event.ManagerUserId != actor.UserId)
            return Result.Failure<IReadOnlyList<TestingSlotRegistrationProjection>>(
                Error.Forbidden("TestingLab.EventManagerRequired", "Only the event manager can list slot registrations."));

        var query = context.Set<TestingSlotRegistration>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.SlotId == request.SlotId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null);
        if (request.Status.HasValue) query = query.Where(candidate => candidate.Status == request.Status.Value);
        var registrations = await query
            .OrderBy(candidate => candidate.Status == TestingSlotRegistrationStatus.Waitlisted
                ? candidate.WaitlistPosition
                : 0)
            .ThenBy(candidate => candidate.RegisteredAt)
            .ThenBy(candidate => candidate.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var result = new List<TestingSlotRegistrationProjection>(registrations.Count);
        foreach (var registration in registrations)
            result.Add(await ToProjectionAsync(registration, cancellationToken).ConfigureAwait(false));
        return Result.Success<IReadOnlyList<TestingSlotRegistrationProjection>>(result);
    }

    public async Task<Result<IReadOnlyList<TestingFeedbackObligationProjection>>> Handle(
        GetMyTestingFeedbackObligationsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null)
            return Result.Failure<IReadOnlyList<TestingFeedbackObligationProjection>>(actor.Error);
        var query = context.Set<TestingFeedbackObligation>().AsNoTracking().Where(candidate =>
            candidate.TesterUserId == actor.UserId &&
            candidate.TenantId == actor.TenantId &&
            candidate.DeletedAt == null);
        if (request.EventId.HasValue) query = query.Where(candidate => candidate.EventId == request.EventId.Value);
        var obligations = await query
            .OrderBy(candidate => candidate.Status)
            .ThenBy(candidate => candidate.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<IReadOnlyList<TestingFeedbackObligationProjection>>(
            obligations.Select(ToProjection).ToList());
    }

    public async Task<Result<IReadOnlyList<TestingSlotRegistrationProjection>>> Handle(
        GetMyTestingSlotRegistrationsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null)
            return Result.Failure<IReadOnlyList<TestingSlotRegistrationProjection>>(actor.Error);
        var query = context.Set<TestingSlotRegistration>()
            .AsNoTracking()
            .Where(registration =>
                registration.UserId == actor.UserId &&
                registration.TenantId == actor.TenantId &&
                registration.DeletedAt == null);
        if (request.EventId.HasValue)
            query = query.Where(registration => registration.EventId == request.EventId.Value);
        var registrations = await query
            .OrderByDescending(registration => registration.RegisteredAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var projections = new List<TestingSlotRegistrationProjection>(registrations.Count);
        foreach (var registration in registrations)
            projections.Add(await ToProjectionAsync(registration, cancellationToken).ConfigureAwait(false));
        return Result.Success<IReadOnlyList<TestingSlotRegistrationProjection>>(projections);
    }

    public async Task<Result<IReadOnlyList<TestingEventFeedbackReviewProjection>>> Handle(
        GetTestingEventFeedbackQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null)
            return Result.Failure<IReadOnlyList<TestingEventFeedbackReviewProjection>>(actor.Error);

        var testingEvent = await context.Set<TestingEvent>()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == request.EventId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        if (testingEvent == null)
            return Result.Failure<IReadOnlyList<TestingEventFeedbackReviewProjection>>(
                Error.NotFound("TestingLab.EventNotFound", "Testing event not found."));
        if (testingEvent.ManagerUserId != actor.UserId)
            return Result.Failure<IReadOnlyList<TestingEventFeedbackReviewProjection>>(
                Error.Forbidden(
                    "TestingLab.EventManagerRequired",
                    "Only the event manager can review event feedback."));

        var obligations = await context.Set<TestingFeedbackObligation>()
            .AsNoTracking()
            .Where(candidate =>
                candidate.EventId == request.EventId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null)
            .OrderBy(candidate => candidate.Status)
            .ThenBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var feedbackIds = obligations
            .Where(candidate => candidate.FeedbackId.HasValue)
            .Select(candidate => candidate.FeedbackId!.Value)
            .ToArray();
        var feedbackById = await context.Set<TestingFeedback>()
            .AsNoTracking()
            .Where(candidate =>
                feedbackIds.Contains(candidate.Id) &&
                candidate.EventId == request.EventId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null)
            .ToDictionaryAsync(candidate => candidate.Id, cancellationToken)
            .ConfigureAwait(false);

        var result = obligations.Select(obligation => new TestingEventFeedbackReviewProjection(
            obligation.Id,
            obligation.EventId,
            obligation.SlotId,
            obligation.ApplicationId,
            obligation.TesterUserId,
            obligation.Status,
            obligation.FulfilledAt,
            obligation.FeedbackId.HasValue &&
            feedbackById.TryGetValue(obligation.FeedbackId.Value, out var feedback)
                ? ToProjection(feedback)
                : null)).ToList();
        return Result.Success<IReadOnlyList<TestingEventFeedbackReviewProjection>>(result);
    }

    private async Task<Result<TestingSlotRegistrationProjection>> ChangeAttendanceAsync(
        Guid registrationId,
        Action<TestingSlotRegistration> transition,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadManagedRegistrationAsync(registrationId, cancellationToken).ConfigureAwait(false);
        if (loaded.Error != null) return Result.Failure<TestingSlotRegistrationProjection>(loaded.Error);
        try
        {
            transition(loaded.Registration!);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(await ToProjectionAsync(loaded.Registration!, cancellationToken).ConfigureAwait(false));
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<TestingSlotRegistrationProjection>(Validation(exception.Message));
        }
    }

    private async Task PromoteWaitlistAsync(Guid slotId, Guid tenantId, CancellationToken cancellationToken)
    {
        var promoted = await context.Set<TestingSlotRegistration>()
            .Where(candidate =>
                candidate.SlotId == slotId &&
                candidate.TenantId == tenantId &&
                candidate.Status == TestingSlotRegistrationStatus.Waitlisted &&
                candidate.DeletedAt == null)
            .OrderBy(candidate => candidate.WaitlistPosition)
            .ThenBy(candidate => candidate.RegisteredAt)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (promoted != null)
        {
            promoted.Promote();
            logger.LogInformation(
                "Promoted tester {TesterId} from the waitlist for Testing Lab slot {SlotId}",
                promoted.UserId,
                slotId);
        }
        await ReindexWaitlistAsync(slotId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReindexWaitlistAsync(Guid slotId, CancellationToken cancellationToken)
    {
        var waitlist = await context.Set<TestingSlotRegistration>()
            .Where(candidate =>
                candidate.SlotId == slotId &&
                candidate.Status == TestingSlotRegistrationStatus.Waitlisted &&
                candidate.DeletedAt == null)
            .OrderBy(candidate => candidate.WaitlistPosition)
            .ThenBy(candidate => candidate.RegisteredAt)
            .ThenBy(candidate => candidate.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        waitlist = waitlist.Where(candidate => candidate.Status == TestingSlotRegistrationStatus.Waitlisted).ToList();
        for (var index = 0; index < waitlist.Count; index++)
        {
            var expectedPosition = index + 1;
            if (waitlist[index].WaitlistPosition != expectedPosition)
                waitlist[index].Reposition(expectedPosition);
        }
    }

    private async Task<LoadedRegistration> LoadRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var actor = await RequireActorAsync(cancellationToken).ConfigureAwait(false);
        if (actor.Error != null) return new(null, null, actor.Error);
        var registration = await context.Set<TestingSlotRegistration>()
            .Include(candidate => candidate.Event)
            .FirstOrDefaultAsync(candidate =>
                candidate.Id == registrationId &&
                candidate.TenantId == actor.TenantId &&
                candidate.DeletedAt == null,
                cancellationToken)
            .ConfigureAwait(false);
        return registration == null
            ? new(null, null, Error.NotFound("TestingLab.SlotRegistrationNotFound", "Testing slot registration not found."))
            : new(registration, actor, null);
    }

    private async Task<LoadedRegistration> LoadManagedRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadRegistrationAsync(registrationId, cancellationToken).ConfigureAwait(false);
        if (loaded.Error != null) return loaded;
        return loaded.Registration!.Event.ManagerUserId == loaded.Actor!.UserId
            ? loaded
            : new(null, null, Error.Forbidden("TestingLab.EventManagerRequired", "Only the event manager can manage attendance."));
    }

    private async Task<ActorScope> RequireActorAsync(CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        var userId = actor.SubjectIdAsGuid;
        if (!actor.IsAuthenticated || userId == null || actor.TenantId == null)
            return new(Guid.Empty, Guid.Empty, Error.Unauthorized(
                "TestingLab.Unauthenticated",
                "An authenticated tenant actor is required."));
        var activeUser = await context.Set<User>().AnyAsync(candidate =>
            candidate.Id == userId.Value &&
            candidate.IsActive &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        var activeMembership = await context.Set<TenantMember>().AnyAsync(candidate =>
            candidate.UserId == userId.Value &&
            candidate.TenantId == actor.TenantId.Value &&
            candidate.IsActive &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        return activeUser && activeMembership
            ? new(userId.Value, actor.TenantId.Value, null)
            : new(Guid.Empty, Guid.Empty, Error.Unauthorized(
                "TestingLab.InactiveActor",
                "An active user and tenant membership are required."));
    }

    private async Task<TestingSlotRegistrationProjection> ToProjectionAsync(
        TestingSlotRegistration registration,
        CancellationToken cancellationToken)
    {
        var pendingFeedback = await context.Set<TestingFeedbackObligation>().CountAsync(candidate =>
            candidate.SlotId == registration.SlotId &&
            candidate.TesterUserId == registration.UserId &&
            candidate.Status == TestingFeedbackObligationStatus.Pending &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        return new TestingSlotRegistrationProjection(
            registration.Id,
            registration.EventId,
            registration.SlotId,
            registration.UserId,
            registration.Status,
            registration.WaitlistPosition,
            registration.Notes,
            registration.RegisteredAt,
            registration.PromotedAt,
            registration.CheckedInAt,
            registration.CheckedOutAt,
            registration.CompletedAt,
            pendingFeedback);
    }


    private async Task<TestingLearningEvidenceCompletedEvent?> CreateLearningEvidenceAsync(
        TestingSlotRegistration registration,
        CancellationToken cancellationToken)
    {
        var testingEvent = registration.Event;
        if (!testingEvent.CourseId.HasValue ||
            !testingEvent.LearningActivityId.HasValue ||
            testingEvent.LearningCompletionRequirement == TestingLearningCompletionRequirement.None)
        {
            return null;
        }

        var hasSubmittedFeedback = await context.Set<TestingFeedbackObligation>().AnyAsync(candidate =>
            candidate.SlotId == registration.SlotId &&
            candidate.TesterUserId == registration.UserId &&
            candidate.Status == TestingFeedbackObligationStatus.Fulfilled &&
            candidate.DeletedAt == null,
            cancellationToken).ConfigureAwait(false);
        var hasPresentedProject = await context.Set<TestingProjectApplication>().AnyAsync(application =>
            application.EventId == registration.EventId &&
            application.AssignedSlotId == registration.SlotId &&
            application.SubmittedByUserId == registration.UserId &&
            application.Status == TestingApplicationStatus.Approved &&
            application.DeletedAt == null &&
            context.Set<TestingFeedbackObligation>().Any(obligation =>
                obligation.ApplicationId == application.Id &&
                obligation.SlotId == registration.SlotId &&
                obligation.DeletedAt == null),
            cancellationToken).ConfigureAwait(false);
        var state = new TestingLearningEvidenceState(
            registration.Status,
            hasSubmittedFeedback,
            hasPresentedProject);
        if (!TestingLearningPolicy.IsSatisfied(testingEvent.LearningCompletionRequirement, state))
        {
            return null;
        }

        return new TestingLearningEvidenceCompletedEvent(
            registration.Id,
            registration.EventId,
            registration.SlotId,
            registration.UserId,
            testingEvent.CourseId.Value,
            testingEvent.CohortId,
            testingEvent.LearningActivityId.Value,
            testingEvent.LearningCompletionRequirement,
            registration.CompletedAt ?? SystemClock.UtcNow,
            registration.TenantId);
    }

    private static TestingFeedbackObligationProjection ToProjection(TestingFeedbackObligation obligation) => new(
        obligation.Id,
        obligation.EventId,
        obligation.SlotId,
        obligation.ApplicationId,
        obligation.TesterUserId,
        obligation.FeedbackId,
        obligation.Status,
        obligation.FulfilledAt);

    private static TestingEventFeedbackProjection ToProjection(TestingFeedback feedback) => new(
        feedback.Id,
        feedback.EventId!.Value,
        feedback.ApplicationId!.Value,
        feedback.UserId,
        feedback.FeedbackData,
        feedback.OverallRating,
        feedback.WouldRecommend,
        feedback.AdditionalNotes,
        feedback.CreatedAt);

    private static Error Validation(string message) => Error.Validation("TestingLab.Validation", message);

    private sealed record ActorScope(Guid UserId, Guid TenantId, Error? Error);

    private sealed record LoadedRegistration(
        TestingSlotRegistration? Registration,
        ActorScope? Actor,
        Error? Error);
}
