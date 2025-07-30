using System.ComponentModel.DataAnnotations;
using GameGuild.Common;


namespace GameGuild.Modules.TestingLab {
  public class TestingFeedbackForm : Entity {
    [Required] public Guid TestingRequestId { get; set; }

    [Required] public string FormSchema { get; set; } = string.Empty; // JSON

    public bool IsForOnline { get; set; } = true;

    public bool IsForSessions { get; set; } = true;
  }
}
