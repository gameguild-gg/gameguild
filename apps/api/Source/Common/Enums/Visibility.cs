using System.ComponentModel;


namespace GameGuild.Common;

public enum Visibility {
  [Description("Content only visible to creators and editors, not published to users")]
  Draft,

  [Description("Content is live and available to all users with access")]
  Published,

  [Description("Content no longer actively shown but preserved for reference")]
  Archived,
}

// public enum VerificationStatus {
//   [Description("Not verified")]
//   NotVerified,

//   [Description("Verified by GameGuild team")]
//   GameGuildVerified,

//   [Description("Verified by community")]
//   CommunityVerified,

//   [Description("Verified by both GameGuild and community")]
//   FullyVerified
// }

// todo: EnrollmentStatus should use permission system instead of this enum
