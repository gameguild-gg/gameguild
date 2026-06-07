namespace GameGuild.TestingLab;

public enum RegistrationStatus {
  Registered = 0,
  Confirmed = 1,
  Cancelled = 2,
  Attended = 3,
  NoShow = 4,
}

public enum ParticipationStatus {
  Registered = 0,
  Active = 1,
  Completed = 2,
  Withdrawn = 3,
  Suspended = 4,
}

public enum FeedbackFormType {
  General = 0,
  BugReport = 1,
  Usability = 2,
  Performance = 3,
  Accessibility = 4,
}

public enum TestingPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}
