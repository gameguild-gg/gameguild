using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Command handler for UnenrollUserCommand
/// </summary>
public class UnenrollUserCommandHandler(IApplicationDbContext context, ILogger<UnenrollUserCommandHandler> logger)
    : ICommandHandler<UnenrollUserCommand, bool>
{
    public async Task<bool> Handle(UnenrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unenrolling user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    var enrollment = await context.ProgramUsers.Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == Guid.Parse(request.UserId) && pu.IsActive).FirstOrDefaultAsync(cancellationToken);

    if (enrollment == null) { return false; }

    enrollment.IsActive = false;
    enrollment.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Unenrolled user {UserId} from program {ProgramId}", request.UserId, request.ProgramId);

    return true;
  }
}
