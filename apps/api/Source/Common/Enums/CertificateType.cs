using System.ComponentModel;


namespace GameGuild.Common;

public enum CertificateType {
  [Description("Standard certificate for completing a single program")]
  ProgramCompletion,

  [Description("Certificate for completing all programs in a product bundle/specialization")]
  ProductBundleCompletion,

  [Description("Certificate for achieving specific skill combinations across multiple programs/products")]
  LearningPathway,

  [Description("Certificate recognizing mastery of specific skills")]
  SkillMastery,

  [Description("Certificate for attending workshops, conferences, or events")]
  EventParticipation,

  [Description("Certificate from passing standardized tests or evaluations")]
  AssessmentPassed,

  [Description("Certificate for successfully completing projects")]
  ProjectCompletion,

  [Description("Certificate for completing a series of related programs")]
  Specialization,

  [Description("Industry-recognized professional certification")]
  Professional,

  [Description("Certificate for specific accomplishments or milestones")]
  Achievement,

  [Description("Certification to teach specific subjects")]
  Instructor,

  [Description("Certificate proving time spent learning a topic")]
  TimeInvestment,

  [Description("Certificate based on peer validation or community contribution")]
  PeerRecognition,
}
