namespace GameGuild.Learning.Assessments;

/// <summary>
/// Submission forms accepted by an assessment. Values are persisted as a bit field and must remain stable.
/// </summary>
[Flags]
public enum SubmissionModality
{
    None = 0,
    Text = 1,
    File = 2,
    Url = 4,
    Code = 8,
    Media = 16,
    Project = 32,
    StructuredAnswer = 64,
}

/// <summary>
/// Defines whether an assessment is delivered one step at a time or as a continuous experience.
/// Values are persisted and must remain stable.
/// </summary>
public enum AssessmentPresentationMode
{
    SingleStep = 0,
    Continuous = 1,
}
