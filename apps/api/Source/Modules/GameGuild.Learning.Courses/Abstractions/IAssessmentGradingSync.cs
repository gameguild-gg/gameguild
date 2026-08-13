namespace GameGuild.Learning.Courses;

public interface IAssessmentGradingSync
{
    Task SyncAsync(Guid contentId, int maxScore, CancellationToken ct = default);
}
