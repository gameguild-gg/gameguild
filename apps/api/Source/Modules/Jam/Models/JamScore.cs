using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Jam.Models {
  public class JamScore : BaseEntity {
    private Guid _submissionId;

    private Guid _criteriaId;

    private Guid _judgeUserId;

    private int _score;

    private string? _feedback;

    [Required]
    public Guid SubmissionId {
      get => _submissionId;
      set => _submissionId = value;
    }

    [Required]
    public Guid CriteriaId {
      get => _criteriaId;
      set => _criteriaId = value;
    }

    [Required]
    public Guid JudgeUserId {
      get => _judgeUserId;
      set => _judgeUserId = value;
    }

    [Required]
    public int Score {
      get => _score;
      set => _score = value;
    }

    public string? Feedback {
      get => _feedback;
      set => _feedback = value;
    }
  }
}
