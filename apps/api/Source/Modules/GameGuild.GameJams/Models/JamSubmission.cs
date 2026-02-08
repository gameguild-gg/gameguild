using System.ComponentModel.DataAnnotations;

namespace GameGuild.GameJams;

public class JamSubmission : EntityBase {
  [Required] public Guid JamId { get; set; }

  [Required] public Guid ProjectVersionId { get; set; }

  [Required] public Guid UserId { get; set; }

  public string? SubmissionNotes { get; set; }
}
