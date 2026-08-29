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
    Draft = 0,
    Pending = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Waitlisted = 5,
    Withdrawn = 6,
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

public enum TestingSlotRegistrationStatus
{
    Registered = 0,
    Waitlisted = 1,
    CheckedIn = 2,
    Attended = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6,
}

[Flags]
public enum TestingLearningCompletionRequirement
{
    None = 0,
    Attendance = 1,
    FeedbackSubmitted = 2,
    ProjectPresented = 4,
}
