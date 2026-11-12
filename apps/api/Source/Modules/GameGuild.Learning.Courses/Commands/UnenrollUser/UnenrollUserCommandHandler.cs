using GameGuild.CQRS;
using GameGuild.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for UnenrollUserCommand
/// </summary>
public sealed class UnenrollUserCommandHandler(IApplicationDbContext context, ILogger<UnenrollUserCommandHandler> logger, IPublisher? publisher = null)
    : ICommandHandler<UnenrollUserCommand, bool>
{
    public async Task<bool> Handle(UnenrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unenrolling user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    var userId = Guid.Parse(request.UserId);
    var enrollment = await context.Set<ProgramUser>().Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == userId && pu.IsActive).FirstOrDefaultAsync(cancellationToken);

    if (enrollment == null) { return false; }

    enrollment.IsActive = false;
    enrollment.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Unenrolled user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    await PublishDroppedAsync(userId, request.ProgramId, cancellationToken).ConfigureAwait(false);

    return true;
  }

    /// <summary>
    ///     Issue #1184 G4: CourseDroppedEvent was declared but never raised; unenrollment is
    ///     its producing site (progress percent is not tracked on the enrollment row — 0 here).
    ///     Separate method so the publish stays outside the persistence method (GGARCH003).
    /// </summary>
    private async Task PublishDroppedAsync(Guid userId, Guid programId, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(new CourseDroppedEvent(userId, programId, null, 0, "unenrolled"), cancellationToken).ConfigureAwait(false);
    }
}
