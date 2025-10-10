using System.ComponentModel;
using System.ComponentModel;

namespace GameGuild.Modules.Feedbacks.Entities;

/// <summary>
/// Categories for organizing feedback submissions
/// </summary>
public enum FeedbackCategory
{
    [Description("General feedback about the program")]
    General = 0,

    [Description("Feedback about course content quality")]
    ContentQuality = 1,

    [Description("Feedback about instructor performance")]
    Instructor = 2,

    [Description("Feedback about platform usability")]
    Platform = 3,

    [Description("Feedback about learning experience")]
    LearningExperience = 4,

    [Description("Suggestions for improvement")]
    Suggestion = 5,

    [Description("Bug reports or technical issues")]
    Technical = 6,

    [Description("Other feedback")]
    Other = 99
}
