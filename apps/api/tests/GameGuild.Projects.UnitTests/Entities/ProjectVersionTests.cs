namespace GameGuild.Projects.UnitTests.Entities;

public sealed class ProjectVersionTests
{
    [Fact]
    public void Create_StartsAsEditableDraft()
    {
        var version = ProjectVersion.Create(
            Guid.NewGuid(),
            "1.0.0",
            "Initial notes",
            Guid.NewGuid(),
            Guid.NewGuid());

        version.Status.Should().Be(ProjectVersionStatus.Draft);

        version.UpdateDraft("1.0.1", "Updated notes");

        version.VersionNumber.Should().Be("1.0.1");
        version.ReleaseNotes.Should().Be("Updated notes");
    }

    [Fact]
    public void ReadyVersion_IsImmutableAndCanBeReleasedThenArchived()
    {
        var version = ProjectVersion.Create(
            Guid.NewGuid(),
            "2.0.0",
            "Testable build",
            Guid.NewGuid(),
            Guid.NewGuid());

        version.MarkReadyForTesting();

        version.Status.Should().Be(ProjectVersionStatus.ReadyForTesting);
        var update = () => version.UpdateDraft("2.0.1", "Mutated");
        update.Should().Throw<InvalidOperationException>();

        version.Release();
        version.Status.Should().Be(ProjectVersionStatus.Released);

        version.Archive();
        version.Status.Should().Be(ProjectVersionStatus.Archived);
    }

    [Theory]
    [InlineData(ProjectVersionStatus.Draft, VersionSubmissionPolicy.ReadyMutableUntilReview, false)]
    [InlineData(ProjectVersionStatus.ReadyForTesting, VersionSubmissionPolicy.ReadyMutableUntilReview, true)]
    [InlineData(ProjectVersionStatus.Released, VersionSubmissionPolicy.ReadyMutableUntilReview, true)]
    [InlineData(ProjectVersionStatus.Archived, VersionSubmissionPolicy.ReadyMutableUntilReview, false)]
    [InlineData(ProjectVersionStatus.Draft, VersionSubmissionPolicy.ReleasedImmutable, false)]
    [InlineData(ProjectVersionStatus.ReadyForTesting, VersionSubmissionPolicy.ReleasedImmutable, false)]
    [InlineData(ProjectVersionStatus.Released, VersionSubmissionPolicy.ReleasedImmutable, true)]
    [InlineData(ProjectVersionStatus.Archived, VersionSubmissionPolicy.ReleasedImmutable, false)]
    public void Eligibility_MatchesConfiguredPolicy(
        ProjectVersionStatus status,
        VersionSubmissionPolicy policy,
        bool expected)
    {
        ProjectVersionEligibility.IsEligible(status, policy).Should().Be(expected);
    }

    [Theory]
    [InlineData(VersionSubmissionPolicy.ReadyMutableUntilReview, true)]
    [InlineData(VersionSubmissionPolicy.ReleasedImmutable, false)]
    public void ReplacementRule_MatchesConfiguredPolicy(VersionSubmissionPolicy policy, bool expected)
    {
        ProjectVersionEligibility.CanReplaceAfterSubmission(policy).Should().Be(expected);
    }
}
