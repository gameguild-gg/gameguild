namespace GameGuild.TestingLab;

/// <summary>
///     Strongly-typed resource type and action constants for TestingLab module.
///     Eliminates magic strings in permission checks and grants.
/// </summary>
/// <remarks>
///     <para>
///         Usage: Instead of using magic strings like "TestingSession" or "create",
///         use <c>TestingLabResourceTypes.Session</c> and <c>TestingLabActions.Create</c>.
///     </para>
/// </remarks>
public static class TestingLabResourceTypes
{
    /// <summary>Testing session resource type</summary>
    public const string Session = "TestingSession";

    /// <summary>Testing location resource type</summary>
    public const string Location = "TestingLocation";

    /// <summary>Testing feedback resource type</summary>
    public const string Feedback = "TestingFeedback";

    /// <summary>Testing request resource type</summary>
    public const string Request = "TestingRequest";

    /// <summary>Testing participant resource type</summary>
    public const string Participant = "TestingParticipant";

    public const string Event = "TestingEvent";

    public const string Application = "TestingProjectApplication";

    public const string Analytics = "TestingLabAnalytics";

    public const string Settings = "TestingLabSettings";

    public const string Template = "TestingEventTemplate";

    /// <summary>
    ///     All TestingLab resource types for validation.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        Session, Location, Feedback, Request, Participant, Event, Application, Analytics, Settings, Template
    };

    /// <summary>
    ///     Validates if a string is a known TestingLab resource type.
    /// </summary>
    public static bool IsValid(string value) =>
        All.Contains(value, StringComparer.Ordinal);
}

/// <summary>
///     Strongly-typed action constants for TestingLab permissions.
/// </summary>
public static class TestingLabActions
{
    /// <summary>Create action</summary>
    public const string Create = "create";

    /// <summary>Read/View action</summary>
    public const string Read = "read";

    /// <summary>Edit/Update action</summary>
    public const string Edit = "edit";

    /// <summary>Delete action</summary>
    public const string Delete = "delete";

    /// <summary>Moderate action (for feedback)</summary>
    public const string Moderate = "moderate";

    /// <summary>Approve action (for requests)</summary>
    public const string Approve = "approve";

    /// <summary>Manage action (for participants)</summary>
    public const string Manage = "manage";

    /// <summary>
    ///     All TestingLab actions for validation.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        Create, Read, Edit, Delete, Moderate, Approve, Manage
    };
}

public static class TestingLabAssetScopes
{
    public const string ApplicationReview = "TestingLab.ApplicationReview";
}
