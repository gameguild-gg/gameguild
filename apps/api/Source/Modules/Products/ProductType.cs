using System.ComponentModel;


namespace GameGuild.Common;

public enum ProductType {
  [Description("Educational content organized into lessons")]
  Program,

  [Description("Curated sequence of programs and skill requirements for specific learning outcomes")]
  LearningPathway,

  [Description("Collection of multiple products")]
  Bundle,

  [Description("Recurring access to content")]
  Subscription,

  [Description("Live interactive sessions")]
  Workshop,
  [Description("One-on-one coaching")] Mentorship,
  [Description("Digital publications")] Ebook,
  [Description("Downloadable assets")] ResourcePack,

  [Description("Access to forums/communities")]
  Community,
  [Description("Industry credentials")] Certification,

  [Description("Future product categories")]
  Other,
}
