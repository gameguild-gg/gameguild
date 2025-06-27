using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;


namespace GameGuild.Modules.Jam.Models {
  public class JamJudgingCriteria : BaseEntity {
    private Guid _jamId;

    private string _name = string.Empty;

    private string? _description;

    private decimal _weight = 1.0m;

    private int _maxScore = 5;

    [Required]
    public Guid JamId {
      get => _jamId;
      set => _jamId = value;
    }

    [Required, MaxLength(100)]
    public string Name {
      get => _name;
      set => _name = value;
    }

    public string? Description {
      get => _description;
      set => _description = value;
    }

    public decimal Weight {
      get => _weight;
      set => _weight = value;
    }

    [Required]
    public int MaxScore {
      get => _maxScore;
      set => _maxScore = value;
    }
  }
}
