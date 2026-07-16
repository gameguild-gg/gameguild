using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments;

public sealed class AssessmentProgramContentLifecycleGuard(IApplicationDbContext context) : IProgramContentLifecycleGuard
{
    public Task<bool> HasBlockingDeleteReference(Guid contentId, CancellationToken cancellationToken = default) =>
        context.Set<InteractiveVideoAssessmentCue>()
            .AnyAsync(cue => cue.ContentId == contentId && cue.DeletedAt == null, cancellationToken);

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
