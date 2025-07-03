using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;
using GameGuild.Modules.Users.Models;


namespace GameGuild.Modules.TestingLab.Models {
  public class TestingParticipant : BaseEntity {
    /// <summary>
    /// Foreign key to the testing request
    /// </summary>
    public Guid TestingRequestId { get; set; }

    /// <summary>
    /// Navigation property to the testing request
    /// </summary>
    public virtual TestingRequest TestingRequest { get; set; } = null!;

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    [Required] public bool InstructionsAcknowledged { get; set; } = false;

    public DateTime? InstructionsAcknowledgedAt { get; set; }

    [Required] public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
  }
}
