using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Programs.Models;
using GameGuild.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Command handler for UpdateEnrollmentStatusCommand
/// </summary>
public class UpdateEnrollmentStatusCommandHandler(IApplicationDbContext context, ILogger<UpdateEnrollmentStatusCommandHandler> logger)
    : ICommandHandler<UpdateEnrollmentStatusCommand, Program>
{
    public async Task<Program> Handle(UpdateEnrollmentStatusCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating enrollment status for program: {ProgramId}", request.ProgramId);

    var program = await context.Set<Program>().Where(p => p.Id == request.ProgramId && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    program.EnrollmentStatus = (EnrollmentStatus)request.Status;
    if (request.MaxEnrollments.HasValue) program.MaxEnrollments = request.MaxEnrollments;
    if (request.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = request.EnrollmentDeadline;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Updated enrollment status for program: {ProgramId}", program.Id);

    return program;
  }
}
