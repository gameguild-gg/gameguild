using GameGuild.Common;


namespace GameGuild.Modules.TestingLab;

internal class FeedbackQualityRating : Entity {
  [Required] public Guid FeedbackId { get; set; }

  [Required] public Guid RatedByUserId { get; set; }

  [Required][Range(1, 5)] public int QualityRating { get; set; }

  public string? Reason { get; set; }
}
