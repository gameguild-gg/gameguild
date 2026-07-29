namespace GameGuild.TestingLab;

public enum TestingEventMode
{
    Online = 0,
    InPerson = 1,
    Hybrid = 2,
}

public enum TestingEventApprovalMode
{
    ManagerOnly = 0,
    Committee = 1,
}

public enum TestingEventStatus
{
    Draft = 0,
    ApplicationsOpen = 1,
    ApplicationsClosed = 2,
    Scheduled = 3,
    Active = 4,
    Completed = 5,
    Cancelled = 6,
}

public enum TestingApplicationStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3,
    Waitlisted = 4,
    Withdrawn = 5,
}

public enum TestingApplicationVoteDecision
{
    Approve = 0,
    Reject = 1,
    Abstain = 2,
}

public enum TestingFeedbackObligationStatus
{
    Pending = 0,
    Fulfilled = 1,
    Waived = 2,
}

[Flags]
public enum TestingLearningCompletionRequirement
{
    None = 0,
    Attendance = 1,
    FeedbackSubmitted = 2,
    ProjectPresented = 4,
}
