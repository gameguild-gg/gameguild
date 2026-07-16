namespace GameGuild.Learning.Assessments;

/// <summary>
/// Links an assessment to a named cue in an interactive-video lesson owned by Learning.Courses.
/// </summary>
public class InteractiveVideoAssessmentCue : EntityBase
{
    public Guid AssessmentId { get; private set; }
    public Guid ContentId { get; private set; }
    public string CueId { get; private set; } = string.Empty;
    public decimal? CuePositionSeconds { get; private set; }
    public Assessment Assessment { get; private set; } = null!;

    private InteractiveVideoAssessmentCue() { }

    public static InteractiveVideoAssessmentCue Create(
        Guid assessmentId,
        Guid contentId,
        string cueId,
        decimal? cuePositionSeconds = null)
    {
        if (assessmentId == Guid.Empty)
        {
            throw new ArgumentException("Assessment ID is required.", nameof(assessmentId));
        }

        if (contentId == Guid.Empty)
        {
            throw new ArgumentException("Content ID is required.", nameof(contentId));
        }

        if (string.IsNullOrWhiteSpace(cueId))
        {
            throw new ArgumentException("Cue ID is required.", nameof(cueId));
        }

        var normalizedCueId = cueId.Trim();
        if (normalizedCueId.Length > 128)
        {
            throw new ArgumentException("Cue ID cannot exceed 128 characters.", nameof(cueId));
        }

        if (cuePositionSeconds is < 0 or > 999999999.999m ||
            (cuePositionSeconds.HasValue && cuePositionSeconds.Value != decimal.Round(cuePositionSeconds.Value, 3)))
        {
            throw new ArgumentOutOfRangeException(nameof(cuePositionSeconds), "Cue position must be non-negative and use at most three decimal places.");
        }

        return new InteractiveVideoAssessmentCue
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            ContentId = contentId,
            CueId = normalizedCueId,
            CuePositionSeconds = cuePositionSeconds,
        };
    }
}
