using GameGuild;


namespace GameGuild.Modules.GameJams.Models {
  public class JamSubmission : EntityBase {
    [Required] public Guid JamId { get; set; }

    [Required] public Guid ProjectVersionId { get; set; }

    [Required] public Guid UserId { get; set; }

    public string? SubmissionNotes { get; set; }
  }
}
