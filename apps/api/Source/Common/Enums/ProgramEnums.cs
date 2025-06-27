using System.ComponentModel;


namespace GameGuild.Common.Enums;

public enum Visibility {
  [Description("Content only visible to creators and editors, not published to users")]
  Draft,

  [Description("Content is live and available to all users with access")]
  Published,

  [Description("Content no longer actively shown but preserved for reference")]
  Archived
}

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
  Other
}

public enum ProgramContentType {
  [Description("Instructional content page")]
  Page,

  [Description("General assignment where students submit work for evaluation")]
  Assignment,

  [Description("A sequence of questions with expected answers, similar to a quiz or test")]
  Questionnaire,

  [Description("Discussion forum for collaborative learning and sharing ideas")]
  Discussion,

  [Description("Programming assignment requiring code submission")]
  Code,

  [Description("Competition-style activity with rankings or achievements")]
  Challenge,

  [Description("Student reflections on learning or experiences")]
  Reflection,

  [Description("Data collection activity without grading")]
  Survey
}

public enum ProgramRoleType {
  [Description("Standard learner role with access to content and ability to submit assignments")]
  Student,

  [Description("Can create content, grade submissions, and manage the program")]
  Instructor,

  [Description("Can edit program content but cannot grade or manage users")]
  Editor,

  [Description("Full control over the program, including user management and settings")]
  Administrator,

  [Description("Can grade submissions and assist students but with limited content editing abilities")]
  TeachingAssistant
}

public enum GradingMethod {
  [Description("Graded manually by an instructor or teaching assistant")]
  Instructor,

  [Description("Peer review-based grading by other students")]
  Peer,

  [Description("Automated grading using AI algorithms")]
  Ai,

  [Description("Graded automatically using predefined test cases")]
  AutomatedTests
}

public enum ModerationStatus {
  [Description("Moderation request submitted but not yet reviewed")]
  Pending,

  [Description("Content approved and can be displayed publicly")]
  Approved,

  [Description("Content rejected and should not be displayed")]
  Rejected,

  [Description("Content flagged for review due to reports or policy violations")]
  Flagged
}

public enum ProgressStatus {
  [Description("User has not started this content yet")]
  NotStarted,

  [Description("User has started but not completed this content")]
  InProgress,

  [Description("User has successfully completed this content")]
  Completed,

  [Description("User has opted to skip this optional content")]
  Skipped
}

public enum ProgramCategory {
  [Description("Programming and software development")]
  Programming,

  [Description("Data science and analytics")]
  DataScience,

  [Description("Web development and design")]
  WebDevelopment,

  [Description("Mobile app development")]
  MobileDevelopment,
  [Description("Game development")] GameDevelopment,

  [Description("Artificial intelligence and machine learning")]
  AI,

  [Description("Cybersecurity and information security")]
  Cybersecurity,

  [Description("DevOps and system administration")]
  DevOps,

  [Description("Database design and management")]
  Database,

  [Description("Business and entrepreneurship")]
  Business,

  [Description("Design and user experience")]
  Design,

  [Description("Marketing and digital marketing")]
  Marketing,
  [Description("Project management")] ProjectManagement,

  [Description("Personal development and soft skills")]
  PersonalDevelopment,

  [Description("Creative arts and media")]
  CreativeArts,

  [Description("Science and mathematics")]
  Science,
  [Description("Language learning")] Language,
  [Description("Other categories")] Other
}

public enum ProgramDifficulty {
  [Description("Suitable for complete beginners")]
  Beginner,

  [Description("Requires basic understanding of the subject")]
  Intermediate,

  [Description("Requires significant prior knowledge and experience")]
  Advanced,

  [Description("Requires expert-level knowledge")]
  Expert
}
