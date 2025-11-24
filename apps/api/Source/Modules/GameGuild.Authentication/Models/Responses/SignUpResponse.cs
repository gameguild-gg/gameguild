namespace GameGuild.Authentication.Models.Responses;

/// <summary>
///     Response for sign-up operations
/// </summary>
public class SignUpResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool RequiresEmailVerification { get; set; }

    public Guid UserId { get; set; }
}
