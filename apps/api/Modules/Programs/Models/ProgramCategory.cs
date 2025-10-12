using System.ComponentModel;


namespace GameGuild;

/// <summary>
/// Educational program categorization for discovery and organization
/// </summary>
/// <remarks>
/// Categories enable learners to find relevant programs and allow the platform
/// to organize content by domain expertise. Each category represents a major
/// field of study with specialized learning paths and career outcomes.
/// </remarks>
public enum ProgramCategory {
  [Description("Programming and software development")] Programming,

  [Description("Data science and analytics")] DataScience,

  [Description("Web development and design")] WebDevelopment,

  [Description("Mobile app development")] MobileDevelopment,

  [Description("Game development")] GameDevelopment,

  [Description("Artificial intelligence and machine learning")] AI,

  [Description("Cybersecurity and information security")] Cybersecurity,

  [Description("DevOps and system administration")] DevOps,

  [Description("Database design and management")] Database,

  [Description("Business and entrepreneurship")] Business,

  [Description("Design and user experience")] Design,

  [Description("Marketing and digital marketing")] Marketing,

  [Description("Project management")] ProjectManagement,

  [Description("Personal development and soft skills")] PersonalDevelopment,

  [Description("Creative arts and media")] CreativeArts,

  [Description("Science and mathematics")] Science,

  [Description("Language learning")] Language,

  [Description("Other categories")] Other,
}
