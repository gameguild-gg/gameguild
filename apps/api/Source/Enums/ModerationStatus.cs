using System.ComponentModel;


namespace GameGuild.Common;

public enum ModerationStatus {
  [Description("Moderation request submitted but not yet reviewed")]
  Pending,

  [Description("Content approved and can be displayed publicly")]
  Approved,

  [Description("Content rejected and should not be displayed")]
  Rejected,

  [Description("Content flagged for review due to reports or policy violations")]
  Flagged,
}
