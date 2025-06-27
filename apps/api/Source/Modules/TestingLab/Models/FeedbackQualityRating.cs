using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.TestingLab.Models {
  public class FeedbackQualityRating : BaseEntity {
    private Guid _feedbackId;

    private Guid _ratedByUserId;

    private int _qualityRating;

    private string? _reason;

    [Required]
    public Guid FeedbackId {
      get => _feedbackId;
      set => _feedbackId = value;
    }

    [Required]
    public Guid RatedByUserId {
      get => _ratedByUserId;
      set => _ratedByUserId = value;
    }

    [Required]
    [Range(1, 5)]
    public int QualityRating {
      get => _qualityRating;
      set => _qualityRating = value;
    }

    public string? Reason {
      get => _reason;
      set => _reason = value;
    }
  }
}
