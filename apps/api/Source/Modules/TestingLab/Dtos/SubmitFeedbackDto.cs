using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.TestingLab {
  /// <summary>
  /// DTO for submitting simple feedback for a testing request
  /// </summary>
  public class SubmitFeedbackDto {
    [Required] public Guid TestingRequestId { get; set; }

    /// <summary>
    /// JSON string containing feedback responses
    /// Key-value pairs where key is the question and value is the response
    /// </summary>
    [Required] public string FeedbackResponses { get; set; } = string.Empty;

    /// <summary>
    /// Overall rating (1-10)
    /// </summary>
    [Range(1, 10)]
    public int? OverallRating { get; set; }

    /// <summary>
    /// Would the tester recommend this game
    /// </summary>
    public bool? WouldRecommend { get; set; }

    /// <summary>
    /// Additional notes from the tester
    /// </summary>
    public string? AdditionalNotes { get; set; }

    /// <summary>
    /// Optional session ID if this feedback was given during a testing session
    /// </summary>
    public Guid? SessionId { get; set; }
  }
}
