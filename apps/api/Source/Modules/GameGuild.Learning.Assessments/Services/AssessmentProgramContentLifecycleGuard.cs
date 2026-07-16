using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments;

public sealed class AssessmentProgramContentLifecycleGuard(IApplicationDbContext context) : IProgramContentLifecycleGuard
{
    public Task<bool> HasBlockingDeleteReference(Guid contentId, CancellationToken cancellationToken = default) =>
        context.Set<InteractiveVideoAssessmentCue>()
            .Join(
                context.Set<Assessment>(),
                cue => cue.AssessmentId,
                assessment => assessment.Id,
                (cue, assessment) => new { cue, assessment })
            .AnyAsync(
                item => item.cue.ContentId == contentId &&
                        item.cue.DeletedAt == null &&
                        item.assessment.DeletedAt == null,
                cancellationToken);

    public Task<bool> HasBlockingIncompatibleUpdateReference(
        Guid contentId,
        ProgramContentType nextType,
        LessonContentFormat? nextLessonFormat,
        CancellationToken cancellationToken = default)
    {
        if (nextType == ProgramContentType.Lesson && nextLessonFormat == LessonContentFormat.Video)
        {
            return Task.FromResult(false);
        }

        return HasBlockingDeleteReference(contentId, cancellationToken);
    }
}
