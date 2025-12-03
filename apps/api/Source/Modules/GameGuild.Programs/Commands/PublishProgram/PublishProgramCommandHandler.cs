using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Command handler for PublishProgramCommand
/// </summary>
public class PublishProgramCommandHandler(IApplicationDbContext context, ILogger<PublishProgramCommandHandler> logger)
    : ICommandHandler<PublishProgramCommand, Program>
{
    public async Task<Program> Handle(PublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Publishing program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Published;
    program.Visibility = AccessLevel.Public;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Published program: {ProgramId}", program.Id);

    return program;
  }
}
