using GameGuild.Common;


namespace GameGuild.Modules.GameJams.Models {
  public class JamSubmission : Entity {
    [Required] public Guid JamId { get; set; }

    [Required] public Guid ProjectVersionId { get; set; }

    [Required] public Guid UserId { get; set; }

    public string? SubmissionNotes { get; set; }
  }
}
