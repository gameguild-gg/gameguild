using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab {
  public class TestingFeedback : Entity {
    /// <summary>
    /// Foreign key to the testing request
    /// </summary>
    public Guid TestingRequestId { get; set; }

    /// <summary>
    /// Navigation property to the testing request
    /// </summary>
    public virtual TestingRequest TestingRequest { get; set; } = null!;

    /// <summary>
    /// Foreign key to the feedback form
    /// </summary>
    public Guid FeedbackFormId { get; set; }

    /// <summary>
    /// Navigation property to the feedback form
    /// </summary>
    public virtual TestingFeedbackForm FeedbackForm { get; set; } = null!;

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Optional foreign key to the session
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Navigation property to the session (optional)
    /// </summary>
    public virtual TestingSession? Session { get; set; }

    [Required] public TestingContext TestingContext { get; set; }

    [Required] public string FeedbackData { get; set; } = string.Empty; // JSON

    /// <summary>
    /// Overall rating (1-10)
    /// </summary>
    [Range(1, 10)]
    public int? OverallRating { get; set; }

    /// <summary>
    /// Would the tester recommend this game
    /// </summary>
    public bool? WouldRecommend { get; set; }

    public string? AdditionalNotes { get; set; }
  }
}
