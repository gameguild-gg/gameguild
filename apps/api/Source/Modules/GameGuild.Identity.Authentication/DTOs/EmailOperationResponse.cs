namespace GameGuild.Identity.Authentication;

/// <summary>
///     Email operation response
/// </summary>
public class EmailOperationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
