namespace GameGuild.Modules.Authentication.Models;

/// <summary>
/// DTO for email operation responses
/// </summary>
public class EmailOperationResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, object>? Data { get; set; }
}
