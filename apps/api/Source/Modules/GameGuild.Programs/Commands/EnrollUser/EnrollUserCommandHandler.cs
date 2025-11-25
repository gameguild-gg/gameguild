using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Command handler for EnrollUserCommand
/// </summary>
public class EnrollUserCommandHandler(IApplicationDbContext context, ILogger<EnrollUserCommandHandler> logger)
    : ICommandHandler<EnrollUserCommand, ProgramUser>
{
    public async Task<ProgramUser> Handle(EnrollUserCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Enrolling user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    var program = await context.Set<Program>().Where(p => p.Id == request.ProgramId && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.ProgramId} not found"); }

    if (!program.IsEnrollmentOpen) { throw new InvalidOperationException("Program enrollment is not open"); }

    // Check if already enrolled
    var existingEnrollment = await context.ProgramUsers.Where(pu => pu.ProgramId == request.ProgramId && pu.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (existingEnrollment != null && existingEnrollment.IsActive) { throw new InvalidOperationException("User is already enrolled in this program"); }

    var enrollment = new ProgramUser { ProgramId = request.ProgramId, UserId = Guid.Parse(request.UserId), JoinedAt = request.EnrollmentDate ?? DateTime.UtcNow, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    context.ProgramUsers.Add(enrollment);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Enrolled user {UserId} in program {ProgramId}", request.UserId, request.ProgramId);

    return enrollment;
  }
}
