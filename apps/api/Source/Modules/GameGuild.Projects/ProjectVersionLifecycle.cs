namespace GameGuild.Projects;

public enum ProjectVersionStatus
{
    Draft,
    ReadyForTesting,
    Released,
    Archived,
}

public enum VersionSubmissionPolicy
{
    ReadyMutableUntilReview,
    ReleasedImmutable,
}

public static class ProjectVersionEligibility
{
    public static bool IsEligible(ProjectVersionStatus status, VersionSubmissionPolicy policy) => policy switch
    {
        VersionSubmissionPolicy.ReadyMutableUntilReview => status is ProjectVersionStatus.ReadyForTesting or ProjectVersionStatus.Released,
        VersionSubmissionPolicy.ReleasedImmutable => status == ProjectVersionStatus.Released,
        _ => false,
    };

    public static bool CanReplaceAfterSubmission(VersionSubmissionPolicy policy) => policy switch
    {
        VersionSubmissionPolicy.ReadyMutableUntilReview => true,
        VersionSubmissionPolicy.ReleasedImmutable => false,
        _ => false,
    };
}
