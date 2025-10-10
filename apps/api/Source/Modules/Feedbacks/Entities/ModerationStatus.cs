using System.ComponentModel;
using System.ComponentModel;

namespace GameGuild.Modules.Feedbacks.Entities;

/// <summary>
/// Status of moderation for ratings and feedback
/// </summary>
public enum ModerationStatus
{
    [Description("Pending moderation review")]
    Pending = 0,

    [Description("Approved and visible")]
    Approved = 1,

    [Description("Rejected and hidden")]
    Rejected = 2,

    [Description("Flagged for review")]
    Flagged = 3
}
