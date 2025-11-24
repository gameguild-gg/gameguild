namespace GameGuild.Authentication.Models.Configuration;

/// <summary>
///     Result of password strength validation.
/// </summary>
public class PasswordStrengthResult
{
    /// <summary>
    ///     Whether the password meets all requirements.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    ///     Overall strength score (0-100).
    /// </summary>
    public int StrengthScore { get; set; }

    /// <summary>
    ///     Strength level description (Weak, Medium, Strong, Very Strong).
    /// </summary>
    public string StrengthLevel { get; set; } = string.Empty;

    /// <summary>
    ///     List of validation failures (which requirements were not met).
    /// </summary>
    public List<string> ValidationFailures { get; set; } = new List<string>();

    /// <summary>
    ///     Suggestions for improving password strength.
    /// </summary>
    public List<string> Suggestions { get; set; } = new List<string>();

    /// <summary>
    ///     Whether password appears in compromised password lists.
    /// </summary>
    public bool IsCompromised { get; set; }

    /// <summary>
    ///     Estimated time to crack this password.
    /// </summary>
    public string? EstimatedCrackTime { get; set; }
}
