namespace GameGuild.Authentication.DTOs;

/// <summary>
///     Email operation response
/// </summary>
public class EmailOperationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
