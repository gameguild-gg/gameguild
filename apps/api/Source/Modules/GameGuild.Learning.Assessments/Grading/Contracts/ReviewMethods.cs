namespace GameGuild.Learning.Assessments.Grading.Contracts;

using System.Text.Json.Serialization;

[Flags]
public enum ReviewMethods
{
    None = 0,
    PeerReview = 1,
    AIReview = 2,
    AutomatedReview = 4,
    InstructorReview = 8,
    SelfReview = 16,
}

[JsonConverter(typeof(JsonStringEnumConverter<ReviewMethod>))]
public enum ReviewMethod
{
    PeerReview,
    AIReview,
    AutomatedReview,
    InstructorReview,
    SelfReview,
}

public static class ReviewMethodsContract
{
    private static readonly HashSet<ReviewMethods> ValidWorkflows =
    [
        ReviewMethods.None,
        ReviewMethods.PeerReview,
        ReviewMethods.AIReview,
        ReviewMethods.AutomatedReview,
        ReviewMethods.InstructorReview,
        ReviewMethods.PeerReview | ReviewMethods.InstructorReview,
        ReviewMethods.AIReview | ReviewMethods.InstructorReview,
        ReviewMethods.AutomatedReview | ReviewMethods.InstructorReview,
        ReviewMethods.SelfReview,
        ReviewMethods.SelfReview | ReviewMethods.InstructorReview,
    ];

    private static readonly ReviewMethods[] PrimaryOrder =
    [
        ReviewMethods.PeerReview,
        ReviewMethods.AIReview,
        ReviewMethods.AutomatedReview,
        ReviewMethods.SelfReview,
    ];

    public static ReviewMethods EnsureValid(this ReviewMethods value, bool allowDraft = false)
    {
        if (!ValidWorkflows.Contains(value) || (!allowDraft && value == ReviewMethods.None))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "ReviewMethods contains an unsupported workflow.");
        }

        return value;
    }

    public static IReadOnlyList<ReviewMethod> ToSequence(this ReviewMethods value)
    {
        value.EnsureValid(allowDraft: true);
        if (value == ReviewMethods.None) return [];
        if (value == ReviewMethods.InstructorReview) return [ReviewMethod.InstructorReview];

        var primary = PrimaryOrder.Single(method => value.HasFlag(method));
        return value.HasFlag(ReviewMethods.InstructorReview)
            ? [ToMethod(primary), ReviewMethod.InstructorReview]
            : [ToMethod(primary)];
    }

    public static ReviewMethod EnsureValid(this ReviewMethod value) =>
        Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "ReviewMethod is unsupported.");

    private static ReviewMethod ToMethod(ReviewMethods value) => value switch
    {
        ReviewMethods.PeerReview => ReviewMethod.PeerReview,
        ReviewMethods.AIReview => ReviewMethod.AIReview,
        ReviewMethods.AutomatedReview => ReviewMethod.AutomatedReview,
        ReviewMethods.InstructorReview => ReviewMethod.InstructorReview,
        ReviewMethods.SelfReview => ReviewMethod.SelfReview,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "ReviewMethods value is not a singular stage."),
    };
}
