using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments.Grading.Code;

/// <summary>Removes the linked assessment inside the existing content deletion transaction.</summary>
public sealed class CodeProgramContentDeleteParticipant(IApplicationDbContext context) : IProgramContentDeleteParticipant
{
    public bool CanHandle(ProgramContent content) => content.Type == ProgramContentType.Code;

    public async Task PrepareDeleteAsync(ProgramContent content, CancellationToken cancellationToken = default)
    {
        var linked = await context.Set<Assessment>()
            .Where(value => value.ContentId == content.Id && value.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var assessment in linked) assessment.SoftDelete();
        context.Set<Assessment>().UpdateRange(linked);
    }
}
