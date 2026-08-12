namespace GameGuild.Learning.Assessments;

/// <summary>
/// Grading strategies that may apply to an assessment. Values are persisted as a bit field and must remain stable.
/// </summary>
[Flags]
public enum AssessmentGradingMethod
{
    None = 0,
    PeerReview = 1,
    AIGraded = 2,
    AutoGraded = 4,
    InstructorGraded = 8,
}
