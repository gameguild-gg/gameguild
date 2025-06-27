using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Jam.Models {
  public class JamSubmission : BaseEntity {
    private Guid _jamId;

    private Guid _projectVersionId;

    private Guid _userId;

    private string? _submissionNotes;

    [Required]
    public Guid JamId {
      get => _jamId;
      set => _jamId = value;
    }

    [Required]
    public Guid ProjectVersionId {
      get => _projectVersionId;
      set => _projectVersionId = value;
    }

    [Required]
    public Guid UserId {
      get => _userId;
      set => _userId = value;
    }

    public string? SubmissionNotes {
      get => _submissionNotes;
      set => _submissionNotes = value;
    }
  }
}
