using System.ComponentModel;


namespace GameGuild.Common;

public enum TagRelationshipType {
  [Description("Tags that are related or complementary")]
  Related,

  [Description("Parent tag in a hierarchical structure")]
  Parent,

  [Description("Child tag in a hierarchical structure")]
  Child,

  [Description("Required prerequisite tag")]
  Requires,

  [Description("Suggested prerequisite or related tag")]
  Suggested,
}
