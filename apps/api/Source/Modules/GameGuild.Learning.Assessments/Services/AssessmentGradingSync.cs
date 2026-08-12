using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments;

public sealed class AssessmentGradingSync(IApplicationDbContext context) : IAssessmentGradingSync
{
    public async Task SyncAsync(Guid contentId, int maxScore, int passingScore, CancellationToken ct = default)
    {
        var assessment = await context.Set<Assessment>()
            .FirstOrDefaultAsync(a => a.ContentId == contentId && a.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (assessment == null) return;

        assessment.SetGrading(maxScore, passingScore);
        context.Set<Assessment>().Update(assessment);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
