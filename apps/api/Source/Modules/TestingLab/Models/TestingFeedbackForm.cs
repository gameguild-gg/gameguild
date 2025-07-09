using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Domain.Entities;


namespace GameGuild.Modules.TestingLab.Models {
  public class TestingFeedbackForm : BaseEntity {
    [Required] public Guid TestingRequestId { get; set; }

    [Required] public string FormSchema { get; set; } = string.Empty; // JSON

    public bool IsForOnline { get; set; } = true;

    public bool IsForSessions { get; set; } = true;
  }
}
