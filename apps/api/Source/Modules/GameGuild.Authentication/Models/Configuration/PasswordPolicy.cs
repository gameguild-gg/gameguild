namespace GameGuild.Authentication.Models.Configuration;

/// <summary>
///     Password policy configuration.
/// </summary>
public abstract class PasswordPolicy
{
    /// <summary>
    ///     Minimum password length.
    /// </summary>
    public int MinLength { get; set; } = 8;

    /// <summary>
    ///     Maximum password length.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    ///     Whether uppercase letters are required.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    ///     Whether lowercase letters are required.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    ///     Whether digits are required.
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    ///     Whether special characters are required.
    /// </summary>
    public bool RequireSpecialChar { get; set; } = true;

    /// <summary>
    ///     Number of unique characters required.
    /// </summary>
    public int? RequireUniqueChars { get; set; }

    /// <summary>
    ///     Number of days before password expires.
    /// </summary>
    public int? PasswordExpirationDays { get; set; }

    /// <summary>
    ///     Number of previous passwords that cannot be reused.
    /// </summary>
    public int? PasswordHistoryCount { get; set; }

    /// <summary>
    ///     Whether to check passwords against common/compromised password lists.
    /// </summary>
    public bool CheckCompromisedPasswords { get; set; } = true;
}
