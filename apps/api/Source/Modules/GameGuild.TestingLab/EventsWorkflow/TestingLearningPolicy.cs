namespace GameGuild.TestingLab;

public sealed record TestingLearningEvidenceState(
    TestingSlotRegistrationStatus RegistrationStatus,
    bool HasSubmittedFeedback,
    bool HasPresentedProject);

public static class TestingLearningPolicy
{
    private const TestingLearningCompletionRequirement SupportedRequirements =
        TestingLearningCompletionRequirement.Attendance |
        TestingLearningCompletionRequirement.FeedbackSubmitted |
        TestingLearningCompletionRequirement.ProjectPresented;

    public static bool IsSatisfied(
        TestingLearningCompletionRequirement requirement,
        TestingLearningEvidenceState state)
    {
        var unsupported = requirement & ~SupportedRequirements;
        if (unsupported != TestingLearningCompletionRequirement.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement,
                "The learning completion requirement contains unsupported flags.");
        }

        if (requirement == TestingLearningCompletionRequirement.None)
        {
            return false;
        }

        if (requirement.HasFlag(TestingLearningCompletionRequirement.Attendance) &&
            state.RegistrationStatus is not (
                TestingSlotRegistrationStatus.Attended or
                TestingSlotRegistrationStatus.Completed))
        {
            return false;
        }

        if (requirement.HasFlag(TestingLearningCompletionRequirement.FeedbackSubmitted) &&
            !state.HasSubmittedFeedback)
        {
            return false;
        }

        return !requirement.HasFlag(TestingLearningCompletionRequirement.ProjectPresented) ||
               state.HasPresentedProject;
    }
}
