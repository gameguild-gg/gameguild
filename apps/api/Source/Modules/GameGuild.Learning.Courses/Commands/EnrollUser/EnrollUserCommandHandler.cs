using GameGuild.CQRS;
using GameGuild.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for EnrollUserCommand
/// </summary>
public sealed class EnrollUserCommandHandler(IApplicationDbContext context, ILogger<EnrollUserCommandHandler> logger, IPublisher? publisher = null)
    : ICommandHandler<EnrollUserCommand, ProgramUser>
{
    public async Task<ProgramUser> Handle(EnrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Enrolling user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    var program = await context.Set<Program>().Where(p => p.Id == request.ProgramId && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    if (!program.IsEnrollmentOpen) { throw new InvalidOperationException("Program enrollment is not open"); }

    // Check if already enrolled
    var existingEnrollment = await context.Set<ProgramUser>().Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (existingEnrollment != null && existingEnrollment.IsActive) { throw new InvalidOperationException("User is already enrolled in this program"); }

    var userId = Guid.Parse(request.UserId);
    var enrollment = new ProgramUser { ProgramId = request.ProgramId, UserId = userId, JoinedAt = request.EnrollmentDate ?? SystemClock.UtcNow, IsActive = true };

    context.Set<ProgramUser>().Add(enrollment);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Enrolled user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    await PublishEnrolledAsync(userId, request.ProgramId, cancellationToken).ConfigureAwait(false);

    return enrollment;
  }

    /// <summary>
    ///     Issue #1184 G4: CourseEnrolledEvent had consumers-to-be but no producer; direct
    ///     enrollments publish it (source "direct") so downstream analytics/recommendation
    ///     handlers can react to enrollments. Separate method so the publish stays outside
    ///     the persistence method (GGARCH003).
    /// </summary>
    private async Task PublishEnrolledAsync(Guid userId, Guid programId, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(new CourseEnrolledEvent(userId, programId, null, "direct"), cancellationToken).ConfigureAwait(false);
    }
}
