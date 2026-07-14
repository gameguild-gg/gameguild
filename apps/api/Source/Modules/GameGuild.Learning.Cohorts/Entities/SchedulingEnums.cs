namespace GameGuild.Learning.Cohorts;

public enum CohortPacingMode
{
    OneModulePerWeek = 0,
    OneLessonPerMeeting = 1,
    FixedLessonsPerWeek = 2,
    Manual = 3
}

public enum CohortReleasePolicy
{
    Weekly = 0,
    BeforeMeeting = 1,
    Manual = 2,
    Immediately = 3
}

public enum CohortScheduleItemType
{
    ContentRelease = 0,
    LiveSession = 1,
    AssessmentWindow = 2,
    Milestone = 3
}

public enum CohortScheduleItemStatus
{
    Draft = 0,
    Scheduled = 1,
    Published = 2,
    Completed = 3,
    Cancelled = 4
}

public enum CohortVisibilityOverride
{
    Inherited = 0,
    Hidden = 1,
    Visible = 2
}

public enum ScheduleShiftScope
{
    Single = 0,
    Following = 1
}

public enum ScheduleConflictSeverity
{
    Advisory = 0,
    Blocking = 1
}
