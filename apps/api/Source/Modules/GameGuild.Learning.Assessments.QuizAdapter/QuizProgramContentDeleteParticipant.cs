using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public sealed class QuizProgramContentDeleteParticipant(IApplicationDbContext context) :
    IProgramContentDeleteParticipant
{
    public bool CanHandle(ProgramContent content) =>
        content.Type == ProgramContentType.Questionnaire;

    public async Task PrepareDeleteAsync(
        ProgramContent content,
        CancellationToken cancellationToken = default)
    {
        var linkedAssessments = await context.Set<Assessment>()
            .Where(value => value.ContentId == content.Id && value.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var assessment in linkedAssessments)
        {
            assessment.SoftDelete();
        }

        context.Set<Assessment>().UpdateRange(linkedAssessments);
    }
}
