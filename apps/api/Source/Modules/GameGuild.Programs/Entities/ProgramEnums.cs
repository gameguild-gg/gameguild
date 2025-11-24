namespace GameGuild.Modules.Programs.Entities;

/// <summary>
/// Program categories for classification and filtering
/// </summary>
public enum ProgramCategory
{
    General = 0,
    Programming = 1,
    Design = 2,
    Business = 3,
    Marketing = 4,
    DataScience = 5,
    Cybersecurity = 6,
    CloudComputing = 7,
    MobileApps = 8,
    WebDevelopment = 9,
    GameDevelopment = 10,
    ArtificialIntelligence = 11,
    DevOps = 12,
    Testing = 13,
    ProjectManagement = 14,
    SoftSkills = 15,
    Leadership = 16,
    Communication = 17
}

/// <summary>
/// Program difficulty levels
/// </summary>
public enum ProgramDifficulty
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
    Expert = 3
}

/// <summary>
/// Program enrollment status
/// </summary>
public enum EnrollmentStatus
{
    Open = 0,
    Closed = 1,
    WaitingList = 2,
    InviteOnly = 3,
    Full = 4
}

/// <summary>
/// Types of program content
/// </summary>
public enum ProgramContentType
{
    Lesson = 0,
    Video = 1,
    Quiz = 2,
    Assignment = 3,
    Project = 4,
    Discussion = 5,
    Reading = 6,
    Lab = 7
}

/// <summary>
/// Grading methods for content
/// </summary>
public enum GradingMethod
{
    None = 0,
    Points = 1,
    Percentage = 2,
    LetterGrade = 3,
    PassFail = 4,
    Rubric = 5,
    PeerReview = 6,
    SelfAssessment = 7
}

/// <summary>
/// Content visibility levels
/// </summary>
public enum Visibility
{
    Public = 0,
    Internal = 1,
    Private = 2,
    Restricted = 3
}