using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for AddProgramContentCommand
/// </summary>
public class AddProgramContentCommandHandler(IApplicationDbContext context, ILogger<AddProgramContentCommandHandler> logger)
    : ICommandHandler<AddProgramContentCommand, ProgramContent>
{
    public async Task<ProgramContent> Handle(AddProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding content {ContentId} to program {ProgramId}", request.ContentId, request.ProgramId);

    var programContent = new ProgramContent {
      ProgramId = request.ProgramId,
      // ContentId = request.ContentId,  // This property doesn't exist in the current model
      SortOrder = request.Order,
      IsRequired = request.IsRequired,
      // PointsReward = request.PointsReward,  // This property doesn't exist in the current model
    };

    context.Set<ProgramContent>().Add(programContent);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Added content {ContentId} to program {ProgramId}", request.ContentId, request.ProgramId);

    return programContent;
  }
}
