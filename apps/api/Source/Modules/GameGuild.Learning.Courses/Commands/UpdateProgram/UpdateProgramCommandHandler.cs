using GameGuild.Abstractions;
using GameGuild.CQRS;



using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Programs;

/// <summary>
/// Command handler for UpdateProgramCommand
/// </summary>
public class UpdateProgramCommandHandler(IApplicationDbContext context, ILogger<UpdateProgramCommandHandler> logger)
    : ICommandHandler<UpdateProgramCommand, Program>
{
    public async Task<Program> Handle(UpdateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    // Update only provided fields
    if (request.Title != null) {
      program.Title = request.Title;
      program.Slug = request.Title.ToSlugCase();
    }

    if (request.Description != null) program.Description = request.Description;
    if (request.Thumbnail != null) program.Thumbnail = request.Thumbnail;
    if (request.VideoShowcaseUrl != null) program.VideoShowcaseUrl = request.VideoShowcaseUrl;
    if (request.EstimatedHours.HasValue) program.EstimatedHours = (int?)request.EstimatedHours;
    if (request.Category.HasValue) program.Category = request.Category.Value;
    if (request.Difficulty.HasValue) program.Difficulty = request.Difficulty.Value;
    if (request.EnrollmentStatus.HasValue) program.EnrollmentStatus = request.EnrollmentStatus.Value;
    if (request.MaxEnrollments.HasValue) program.MaxEnrollments = request.MaxEnrollments;
    if (request.EnrollmentDeadline.HasValue) program.EnrollmentDeadline = request.EnrollmentDeadline;

    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Updated program: {ProgramId}", program.Id);

    return program;
  }
}
