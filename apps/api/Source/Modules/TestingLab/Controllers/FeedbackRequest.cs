namespace GameGuild.Modules.TestingLab;

public class FeedbackRequest {
  public Guid FeedbackFormId { get; set; }

  public string FeedbackData { get; set; } = string.Empty;

  public TestingContext TestingContext { get; set; }

  public Guid? SessionId { get; set; }

  public string? AdditionalNotes { get; set; }
}
