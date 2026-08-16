using GameGuild.CQRS;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for PublishProgramCommand
/// </summary>
public sealed class PublishProgramCommandHandler(IApplicationDbContext context, ILogger<PublishProgramCommandHandler> logger, Social.Posts.Services.IPublicationAnnouncer? publicationAnnouncer = null)
    : ICommandHandler<PublishProgramCommand, Program>
{
    public async Task<Program> Handle(PublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Publishing program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Published;
    program.Visibility = ContentVisibility.Public;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Published program: {ProgramId}", program.Id);

    if (publicationAnnouncer is not null && program.CreatorId is { } creatorId)
    {
        await publicationAnnouncer.AnnounceCoursePublishedAsync(creatorId, program.Title, program.Id, program.TenantId, cancellationToken).ConfigureAwait(false);
    }

    return program;
  }
}
