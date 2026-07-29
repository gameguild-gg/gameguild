using FluentAssertions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLearningPolicyTests
{
    [Theory]
    [InlineData(TestingSlotRegistrationStatus.Registered, false)]
    [InlineData(TestingSlotRegistrationStatus.CheckedIn, false)]
    [InlineData(TestingSlotRegistrationStatus.Attended, true)]
    [InlineData(TestingSlotRegistrationStatus.Completed, true)]
    public void IsSatisfied_WhenAttendanceIsRequired_ShouldRequireRecordedAttendance(
        TestingSlotRegistrationStatus status,
        bool expected)
    {
        var state = new TestingLearningEvidenceState(
            status,
            HasSubmittedFeedback: false,
            HasPresentedProject: false);

        TestingLearningPolicy.IsSatisfied(
                TestingLearningCompletionRequirement.Attendance,
                state)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void IsSatisfied_WhenFeedbackIsRequired_ShouldNotAcceptWaivedObligations()
    {
        var state = new TestingLearningEvidenceState(
            TestingSlotRegistrationStatus.Attended,
            HasSubmittedFeedback: false,
            HasPresentedProject: false);

        TestingLearningPolicy.IsSatisfied(
                TestingLearningCompletionRequirement.FeedbackSubmitted,
                state)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void IsSatisfied_WhenProjectPresentationIsRequired_ShouldRequirePresentationEvidence()
    {
        var missingPresentation = new TestingLearningEvidenceState(
            TestingSlotRegistrationStatus.Attended,
            HasSubmittedFeedback: true,
            HasPresentedProject: false);
        var presented = missingPresentation with { HasPresentedProject = true };

        TestingLearningPolicy.IsSatisfied(
                TestingLearningCompletionRequirement.ProjectPresented,
                missingPresentation)
            .Should()
            .BeFalse();
        TestingLearningPolicy.IsSatisfied(
                TestingLearningCompletionRequirement.ProjectPresented,
                presented)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsSatisfied_WhenRequirementsAreCombined_ShouldRequireEveryEvidence()
    {
        const TestingLearningCompletionRequirement requirement =
            TestingLearningCompletionRequirement.Attendance |
            TestingLearningCompletionRequirement.FeedbackSubmitted |
            TestingLearningCompletionRequirement.ProjectPresented;

        TestingLearningPolicy.IsSatisfied(
                requirement,
                new TestingLearningEvidenceState(
                    TestingSlotRegistrationStatus.Completed,
                    HasSubmittedFeedback: true,
                    HasPresentedProject: false))
            .Should()
            .BeFalse();
        TestingLearningPolicy.IsSatisfied(
                requirement,
                new TestingLearningEvidenceState(
                    TestingSlotRegistrationStatus.Completed,
                    HasSubmittedFeedback: true,
                    HasPresentedProject: true))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void IsSatisfied_WhenRequirementContainsUnknownFlags_ShouldRejectIt()
    {
        var requirement = (TestingLearningCompletionRequirement)8;

        var act = () => TestingLearningPolicy.IsSatisfied(
            requirement,
            new TestingLearningEvidenceState(
                TestingSlotRegistrationStatus.Completed,
                HasSubmittedFeedback: true,
                HasPresentedProject: true));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
