namespace GameGuild.Learning.Courses;

public interface IAssessmentGradingSync
{
    Task SyncAsync(Guid contentId, int maxScore, int passingScore, CancellationToken ct = default);
}
