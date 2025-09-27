namespace GameGuild.Modules.Authentication;

/// <summary>
/// Generic response DTO for email operations
/// </summary>
public class EmailOperationResponse {
  public bool Success { get; set; }

  public string Message { get; set; } = string.Empty;
}
