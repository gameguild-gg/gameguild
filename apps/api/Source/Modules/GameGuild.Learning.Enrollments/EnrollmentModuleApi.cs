using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Enrollments;

public sealed record EnrollmentDto(
    Guid Id,
    Guid CourseId,
    Guid UserId,
    Guid? CohortId,
    EnrollmentStatus Status,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    DateTime? DroppedAt,
    int Progress,
    DateTime? LastActivityAt);

public sealed record EnrollUserRequest(Guid CourseId, Guid UserId, Guid? CohortId = null);

public sealed record UpdateEnrollmentProgressRequest(int Progress);

public sealed record EnrollUserCommand(Guid CourseId, Guid UserId, Guid? CohortId) : ICommand<Result<EnrollmentDto>>;

public sealed record UpdateEnrollmentProgressCommand(Guid EnrollmentId, int Progress) : ICommand<EnrollmentDto?>;

public sealed record SetEnrollmentStatusCommand(Guid EnrollmentId, EnrollmentStatus Status) : ICommand<EnrollmentDto?>;

public sealed record GetEnrollmentQuery(Guid EnrollmentId) : IQuery<EnrollmentDto?>;

public sealed record GetUserEnrollmentsQuery(Guid UserId, EnrollmentStatus? Status = null) : IQuery<IReadOnlyList<EnrollmentDto>>;

public sealed record GetCourseEnrollmentsQuery(Guid CourseId, EnrollmentStatus? Status = null) : IQuery<IReadOnlyList<EnrollmentDto>>;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Enrollment?> GetActiveAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Enrollment>> GetByUserAsync(Guid userId, EnrollmentStatus? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Enrollment>> GetByCourseAsync(Guid courseId, EnrollmentStatus? status, CancellationToken cancellationToken = default);

    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
}

public sealed class EnrollmentRepository(IApplicationDbContext context) : IEnrollmentRepository
{
    public Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Set<Enrollment>().FirstOrDefaultAsync(enrollment => enrollment.Id == id, cancellationToken);

    public Task<Enrollment?> GetActiveAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
        => context.Set<Enrollment>()
            .FirstOrDefaultAsync(
                enrollment => enrollment.CourseId == courseId &&
                              enrollment.UserId == userId &&
                              enrollment.Status != EnrollmentStatus.Dropped &&
                              enrollment.Status != EnrollmentStatus.Expired,
                cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetByUserAsync(Guid userId, EnrollmentStatus? status, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Enrollment>().Where(enrollment => enrollment.UserId == userId);
        if (status.HasValue)
        {
            query = query.Where(enrollment => enrollment.Status == status.Value);
        }

        return await query.OrderByDescending(enrollment => enrollment.EnrolledAt).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByCourseAsync(Guid courseId, EnrollmentStatus? status, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Enrollment>().Where(enrollment => enrollment.CourseId == courseId);
        if (status.HasValue)
        {
            query = query.Where(enrollment => enrollment.Status == status.Value);
        }

        return await query.OrderByDescending(enrollment => enrollment.EnrolledAt).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        context.Set<Enrollment>().Update(enrollment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public interface IEnrollmentService
{
    Task<Result<EnrollmentDto>> EnrollAsync(EnrollUserCommand command, CancellationToken cancellationToken = default);

    Task<EnrollmentDto?> UpdateProgressAsync(Guid id, int progress, CancellationToken cancellationToken = default);

    Task<EnrollmentDto?> SetStatusAsync(Guid id, EnrollmentStatus status, CancellationToken cancellationToken = default);

    Task<EnrollmentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentDto>> GetUserEnrollmentsAsync(Guid userId, EnrollmentStatus? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentDto>> GetCourseEnrollmentsAsync(Guid courseId, EnrollmentStatus? status, CancellationToken cancellationToken = default);
}

public sealed class EnrollmentService(
    IEnrollmentRepository repository,
    IApplicationDbContext context,
    ILearningAuditEventSink auditSink,
    IRequestContextAccessor? requestContextAccessor = null,
    ICohortEnrollmentGuard? cohortEnrollmentGuard = null) : IEnrollmentService
{
    private Guid? ActingUserId => requestContextAccessor?.CurrentUserId;

    public async Task<Result<EnrollmentDto>> EnrollAsync(EnrollUserCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetActiveAsync(command.CourseId, command.UserId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Success(ToDto(existing));
        }

        // Cohort gate (issue #1226 N17): a cohort-tagged enrollment must pass
        // Cohort.CanEnroll() and reserve one seat before the enrollment row exists.
        // The reservation is staged on the shared scoped DbContext so the same save
        // (and transaction) persists both; without a configured guard the path fails
        // closed rather than storing an ungated CohortId.
        if (command.CohortId is Guid cohortId)
        {
            if (cohortEnrollmentGuard is null)
            {
                return Result.Failure<EnrollmentDto>(
                    Error.Failure("Cohort", "Cohort enrollment is not available"));
            }

            var reservation = await cohortEnrollmentGuard
                .ReserveCapacityAsync(cohortId, cancellationToken).ConfigureAwait(false);
            if (!reservation.IsSuccess)
            {
                return Result.Failure<EnrollmentDto>(reservation.Error);
            }
        }

        var enrollment = Enrollment.Create(command.CourseId, command.UserId, command.CohortId);
        try
        {
            // The enrollment row, the cohort seat reservation and the audit event share
            // one transaction: an audit failure (or a failed save) rolls everything back
            // instead of committing an unaudited mutation or a leaked seat.
            var transaction = await context.BeginAuditedMutationTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await repository.AddAsync(enrollment, cancellationToken).ConfigureAwait(false);
                await auditSink.RecordAsync(new LearningAuditEvent(
                    "Learning.Enrollments.UserEnrolled", "Enrollment", enrollment.Id.ToString(),
                    ActingUserId ?? command.UserId, enrollment.TenantId,
                    $"User enrolled in course {command.CourseId}",
                    Metadata: new { command.CourseId, command.UserId, command.CohortId, Status = enrollment.Status.ToString() }), cancellationToken).ConfigureAwait(false);
                await transaction.CommitAuditedMutationAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another enrollee won the race for the last seat (the cohort's optimistic
            // concurrency version changed between load and save): report a capacity
            // conflict instead of an overshoot.
            return Result.Failure<EnrollmentDto>(
                Error.Conflict("Cohort", "Cohort capacity changed concurrently; retry the enrollment"));
        }

        return Result.Success(ToDto(enrollment));
    }

    public async Task<EnrollmentDto?> UpdateProgressAsync(Guid id, int progress, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (enrollment is null)
        {
            return null;
        }

        enrollment.UpdateProgress(progress);
        await SaveWithAuditAsync(enrollment, "Learning.Enrollments.ProgressUpdated",
            $"Enrollment progress updated to {progress}%", new { Progress = progress }, cancellationToken).ConfigureAwait(false);
        return ToDto(enrollment);
    }

    public async Task<EnrollmentDto?> SetStatusAsync(Guid id, EnrollmentStatus status, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (enrollment is null)
        {
            return null;
        }

        var previousStatus = enrollment.Status;

        switch (status)
        {
            case EnrollmentStatus.Active:
                enrollment.Resume();
                break;
            case EnrollmentStatus.Paused:
                enrollment.Pause();
                break;
            case EnrollmentStatus.Completed:
                enrollment.Complete();
                break;
            case EnrollmentStatus.Dropped:
                enrollment.Drop();
                break;
            case EnrollmentStatus.Expired:
                enrollment.Drop();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        // Capacity accounting (issue #1226 N17): leaving via drop/expire frees the
        // reserved seat. The decrement is staged on the shared context so the same
        // transaction persists it with the status change; it is deliberately
        // compensating-best-effort — a vanished cohort row must not block a drop.
        if (status is EnrollmentStatus.Dropped or EnrollmentStatus.Expired
            && previousStatus is EnrollmentStatus.Active or EnrollmentStatus.Paused
            && enrollment.CohortId is Guid releasedCohortId
            && cohortEnrollmentGuard is not null)
        {
            await cohortEnrollmentGuard
                .ReleaseCapacityAsync(releasedCohortId, cancellationToken).ConfigureAwait(false);
        }

        await SaveWithAuditAsync(
            enrollment,
            "Learning.Enrollments.StatusChanged",
            $"Enrollment status changed to {status}",
            new { Status = status.ToString() },
            cancellationToken,
            completionActionType: status == EnrollmentStatus.Completed ? "Learning.Enrollments.EnrollmentCompleted" : null,
            completionDescription: $"Enrollment completed for course {enrollment.CourseId}").ConfigureAwait(false);
        return ToDto(enrollment);
    }

    private async Task SaveWithAuditAsync(
        Enrollment enrollment,
        string actionType,
        string description,
        object metadata,
        CancellationToken cancellationToken,
        string? completionActionType = null,
        string? completionDescription = null)
    {
        var transaction = await context.BeginAuditedMutationTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await repository.UpdateAsync(enrollment, cancellationToken).ConfigureAwait(false);
            await auditSink.RecordAsync(new LearningAuditEvent(
                actionType, "Enrollment", enrollment.Id.ToString(), ActingUserId, enrollment.TenantId,
                description, Metadata: metadata), cancellationToken).ConfigureAwait(false);
            // Completion is the program-completion event of its own lifecycle (issue #180 program
            // management tracking): a dedicated EnrollmentCompleted event is journaled in the same
            // transaction as the status change so completion reporting never depends on parsing
            // StatusChanged payloads.
            if (completionActionType is not null)
            {
                await auditSink.RecordAsync(new LearningAuditEvent(
                    completionActionType, "Enrollment", enrollment.Id.ToString(), ActingUserId, enrollment.TenantId,
                    completionDescription, Metadata: metadata), cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAuditedMutationAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task<EnrollmentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return enrollment is null ? null : ToDto(enrollment);
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetUserEnrollmentsAsync(Guid userId, EnrollmentStatus? status, CancellationToken cancellationToken = default)
        => (await repository.GetByUserAsync(userId, status, cancellationToken).ConfigureAwait(false)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<EnrollmentDto>> GetCourseEnrollmentsAsync(Guid courseId, EnrollmentStatus? status, CancellationToken cancellationToken = default)
        => (await repository.GetByCourseAsync(courseId, status, cancellationToken).ConfigureAwait(false)).Select(ToDto).ToList();

    private static EnrollmentDto ToDto(Enrollment enrollment)
        => new(
            enrollment.Id,
            enrollment.CourseId,
            enrollment.UserId,
            enrollment.CohortId,
            enrollment.Status,
            enrollment.EnrolledAt,
            enrollment.CompletedAt,
            enrollment.DroppedAt,
            enrollment.Progress,
            enrollment.LastActivityAt);
}

public sealed class EnrollUserCommandHandler(IEnrollmentService service) : ICommandHandler<EnrollUserCommand, Result<EnrollmentDto>>
{
    public Task<Result<EnrollmentDto>> Handle(EnrollUserCommand request, CancellationToken cancellationToken)
        => service.EnrollAsync(request, cancellationToken);
}

public sealed class UpdateEnrollmentProgressCommandHandler(IEnrollmentService service) : ICommandHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>
{
    public Task<EnrollmentDto?> Handle(UpdateEnrollmentProgressCommand request, CancellationToken cancellationToken)
        => service.UpdateProgressAsync(request.EnrollmentId, request.Progress, cancellationToken);
}

public sealed class SetEnrollmentStatusCommandHandler(IEnrollmentService service) : ICommandHandler<SetEnrollmentStatusCommand, EnrollmentDto?>
{
    public Task<EnrollmentDto?> Handle(SetEnrollmentStatusCommand request, CancellationToken cancellationToken)
        => service.SetStatusAsync(request.EnrollmentId, request.Status, cancellationToken);
}

public sealed class GetEnrollmentQueryHandler(IEnrollmentService service) : IQueryHandler<GetEnrollmentQuery, EnrollmentDto?>
{
    public Task<EnrollmentDto?> Handle(GetEnrollmentQuery request, CancellationToken cancellationToken)
        => service.GetAsync(request.EnrollmentId, cancellationToken);
}

public sealed class GetUserEnrollmentsQueryHandler(IEnrollmentService service) : IQueryHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>
{
    public Task<IReadOnlyList<EnrollmentDto>> Handle(GetUserEnrollmentsQuery request, CancellationToken cancellationToken)
        => service.GetUserEnrollmentsAsync(request.UserId, request.Status, cancellationToken);
}

public sealed class GetCourseEnrollmentsQueryHandler(IEnrollmentService service) : IQueryHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>
{
    public Task<IReadOnlyList<EnrollmentDto>> Handle(GetCourseEnrollmentsQuery request, CancellationToken cancellationToken)
        => service.GetCourseEnrollmentsAsync(request.CourseId, request.Status, cancellationToken);
}

[ApiController]
[Route("api/learning/enrollments")]
[Authorize]
public sealed class EnrollmentsController(
    ISender sender,
    IActorContextAccessor actorContextAccessor,
    ICourseAccessEvaluator courseAccessEvaluator) : ControllerBase
{
    /// <summary>
    ///     Actor gate for user-scoped data (issue #1226 N6): a learner may only touch
    ///     their own enrollments; system admins keep cross-user access for support.
    ///     Mirrors the module's sibling ownership checks (AssessmentsController).
    /// </summary>
    private bool CanAccessUserData(Guid userId)
    {
        var actor = actorContextAccessor.ActorContext;
        return actor.IsSystemAdmin || actor.SubjectIdAsGuid == userId;
    }

    private async Task<bool> CanReadRosterAsync(Guid courseId, CancellationToken cancellationToken)
    {
        // Course rosters are staff data: reading every learner's enrollment for a
        // course requires the course Edit capability (the evaluator grants system
        // admins everything).
        return await courseAccessEvaluator
            .HasCapabilityAsync(courseId, CourseCapability.Edit, cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var enrollment = await sender.Send(new GetEnrollmentQuery(id), cancellationToken).ConfigureAwait(false);
        if (enrollment is null)
        {
            return NotFound();
        }

        if (!CanAccessUserData(enrollment.UserId))
        {
            return Forbid();
        }

        return Ok(enrollment);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] EnrollmentStatus? status, CancellationToken cancellationToken)
    {
        if (!CanAccessUserData(userId))
        {
            return Forbid();
        }

        var enrollments = await sender.Send(new GetUserEnrollmentsQuery(userId, status), cancellationToken)
            .ConfigureAwait(false);
        return Ok(enrollments);
    }

    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetByCourse(Guid courseId, [FromQuery] EnrollmentStatus? status, CancellationToken cancellationToken)
    {
        if (!await CanReadRosterAsync(courseId, cancellationToken).ConfigureAwait(false))
        {
            return Forbid();
        }

        var enrollments = await sender.Send(new GetCourseEnrollmentsQuery(courseId, status), cancellationToken)
            .ConfigureAwait(false);
        return Ok(enrollments);
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(EnrollUserRequest request, CancellationToken cancellationToken)
    {
        // Ownership gate (issue #1226 N6): enroll the acting user, or anybody only
        // when the actor is a system admin — a body-supplied UserId may no longer
        // target an arbitrary learner.
        if (!CanAccessUserData(request.UserId))
        {
            return Forbid();
        }

        var result = await sender.Send(
            new EnrollUserCommand(request.CourseId, request.UserId, request.CohortId), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Forbidden => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, UpdateEnrollmentProgressRequest request, CancellationToken cancellationToken)
    {
        // Ownership gate (issue #1226 N6): progress may only be mutated for the acting
        // user's own enrollment (or by a system admin).
        var owned = await sender.Send(new GetEnrollmentQuery(id), cancellationToken).ConfigureAwait(false);
        if (owned is null)
        {
            return NotFound();
        }

        if (!CanAccessUserData(owned.UserId))
        {
            return Forbid();
        }

        var enrollment = await sender.Send(new UpdateEnrollmentProgressCommand(id, request.Progress), cancellationToken)
            .ConfigureAwait(false);
        return enrollment is null ? NotFound() : Ok(enrollment);
    }

    [HttpPost("{id:guid}/status/{status}")]
    public async Task<IActionResult> SetStatus(Guid id, EnrollmentStatus status, CancellationToken cancellationToken)
    {
        // Ownership gate (issue #1226 N6): status may only be mutated for the acting
        // user's own enrollment (or by a system admin).
        var owned = await sender.Send(new GetEnrollmentQuery(id), cancellationToken).ConfigureAwait(false);
        if (owned is null)
        {
            return NotFound();
        }

        if (!CanAccessUserData(owned.UserId))
        {
            return Forbid();
        }

        var enrollment = await sender.Send(new SetEnrollmentStatusCommand(id, status), cancellationToken)
            .ConfigureAwait(false);
        return enrollment is null ? NotFound() : Ok(enrollment);
    }
}

public sealed class EnrollmentsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
    }
}

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("learning_enrollments");
        builder.HasKey(enrollment => enrollment.Id);
        builder.Property(enrollment => enrollment.Status).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(enrollment => new { enrollment.CourseId, enrollment.UserId });
        builder.HasIndex(enrollment => enrollment.UserId);
        builder.HasIndex(enrollment => enrollment.CourseId);
        builder.HasIndex(enrollment => enrollment.Status);
    }
}

public static class EnrollmentsDependencyInjection
{
    public static IServiceCollection AddLearningEnrollmentsModule(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<GameGuild.Learning.Courses.ICourseEnrollmentAccessReader, CourseEnrollmentAccessReader>();
        services.AddScoped<ICommandHandler<EnrollUserCommand, Result<EnrollmentDto>>, EnrollUserCommandHandler>();
        services.AddScoped<IRequestHandler<EnrollUserCommand, Result<EnrollmentDto>>>(sp => sp.GetRequiredService<ICommandHandler<EnrollUserCommand, Result<EnrollmentDto>>>());
        services.AddScoped<ICommandHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>, UpdateEnrollmentProgressCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>>(sp => sp.GetRequiredService<ICommandHandler<UpdateEnrollmentProgressCommand, EnrollmentDto?>>());
        services.AddScoped<ICommandHandler<SetEnrollmentStatusCommand, EnrollmentDto?>, SetEnrollmentStatusCommandHandler>();
        services.AddScoped<IRequestHandler<SetEnrollmentStatusCommand, EnrollmentDto?>>(sp => sp.GetRequiredService<ICommandHandler<SetEnrollmentStatusCommand, EnrollmentDto?>>());
        services.AddScoped<IQueryHandler<GetEnrollmentQuery, EnrollmentDto?>, GetEnrollmentQueryHandler>();
        services.AddScoped<IRequestHandler<GetEnrollmentQuery, EnrollmentDto?>>(sp => sp.GetRequiredService<IQueryHandler<GetEnrollmentQuery, EnrollmentDto?>>());
        services.AddScoped<IQueryHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>, GetUserEnrollmentsQueryHandler>();
        services.AddScoped<IRequestHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>(sp => sp.GetRequiredService<IQueryHandler<GetUserEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>());
        services.AddScoped<IQueryHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>, GetCourseEnrollmentsQueryHandler>();
        services.AddScoped<IRequestHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>(sp => sp.GetRequiredService<IQueryHandler<GetCourseEnrollmentsQuery, IReadOnlyList<EnrollmentDto>>>());
        return services;
    }
}

public sealed class LearningEnrollmentsModule : ModuleBase
{
    public override string Name => "Learning.Enrollments";
    public override int Order => 140;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddLearningEnrollmentsModule();

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}
