namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     Response for email operations
/// </summary>
public class EmailOperationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
