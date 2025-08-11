namespace GameGuild.Modules.Permissions;

public enum PermissionType {
  #region Interaction Permissions

  Read = 1,
  Comment = 2,
  Reply = 3,
  Vote = 4,
  Share = 5,
  Report = 6,
  Follow = 7,
  Bookmark = 8,
  React = 9,
  Subscribe = 10,
  Mention = 11,
  Tag = 12,

  #endregion

  #region Curation Permissions

  Categorize = 13,
  Collection = 14,
  Series = 15,
  CrossReference = 16,
  Translate = 17,
  Version = 18,
  Template = 19,

  #endregion

  #region Lifecycle Permissions

  Create = 20,
  Draft = 21,
  Submit = 22,
  Withdraw = 23,
  Archive = 24,
  Restore = 25,
  Delete = 26, // Delete is an alias for SoftDelete, so they share the same value

  SoftDelete =
    26, // Only the owners of a resource can soft delete it at resource level, it still can be deleted by admins at tenant or content type level

  HardDelete = 27,
  Backup = 28,
  Migrate = 29,
  Clone = 30,

  #endregion

  #region Editorial Permissions

  Edit = 31,
  Proofread = 32,
  FactCheck = 33,
  StyleGuide = 34,
  Plagiarism = 35,
  Seo = 36,
  Accessibility = 37,
  Legal = 38,
  Brand = 39,
  Guidelines = 40,

  #endregion

  #region Moderation Permissions

  Review = 41,
  Approve = 42,
  Reject = 43,
  Hide = 44,
  Quarantine = 45,
  Flag = 46,
  Warning = 47,
  Suspend = 48,
  Ban = 49,
  Escalate = 50,

  #endregion

  #region Monetization Permissions

  Monetize = 51,
  Paywall = 52,
  Subscription = 53,
  Advertisement = 54,
  Sponsorship = 55,
  Affiliate = 56,
  Commission = 57,
  License = 58,
  Pricing = 59,
  Revenue = 60,

  #endregion

  #region Promotion Permissions

  Feature = 61,
  Pin = 62,
  Trending = 63,
  Recommend = 64,
  Spotlight = 65,
  Banner = 66,
  Carousel = 67,
  Widget = 68,
  Email = 69,
  Push = 70,
  Sms = 71,

  #endregion

  #region Publishing Permissions

  Publish = 72,
  Unpublish = 73,
  Schedule = 74,
  Reschedule = 75,
  Distribute = 76,
  Syndicate = 77,
  Rss = 78,
  Newsletter = 79,
  SocialMedia = 80,
  Api = 81,

  #endregion

  #region Quality Control Permissions

  Score = 82,
  Rate = 83,
  Benchmark = 84,
  Metrics = 85,
  Analytics = 86,
  Performance = 87,
  Feedback = 88,
  Audit = 89,
  Standards = 90,
  Improvement = 91,

  #endregion
}
