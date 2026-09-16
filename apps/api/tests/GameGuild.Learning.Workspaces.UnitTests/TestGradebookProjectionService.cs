using GameGuild.Learning.Assessments;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Persistence;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Grading.Contracts;

namespace GameGuild.Learning.Workspaces.UnitTests;

internal sealed class TestGradebookProjectionService(
    Func<Guid, Guid, bool, GradebookCourseProjectionV1>? projection = null)
    : IAssessmentGradebookProjectionService
{
    public Task ProjectFinalizedResultAsync(
        AssessmentSubmission submission,
        Assessment assessment,
        GradeRound round,
        GradeResultV1 result,
        IReadOnlyList<Guid> enrollmentIds,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ReprojectAssessmentPlacementAsync(
        Guid assessmentId,
        Guid actorId,
        Guid? previousGroupId,
        PercentValue? previousWeight,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ReprojectGroupWeightAsync(
        Guid assessmentGroupId,
        Guid actorId,
        PercentValue previousWeight,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RemoveAssessmentGroupPlacementAsync(
        Guid assessmentGroupId,
        Guid actorId,
        PercentValue previousWeight,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<GradebookCourseProjectionV1> GetCourseProjectionAsync(
        Guid courseId,
        Guid enrollmentId,
        bool learnerView,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(projection?.Invoke(courseId, enrollmentId, learnerView) ??
                        new GradebookCourseProjectionV1(courseId, enrollmentId, true, false, null, []));
}
