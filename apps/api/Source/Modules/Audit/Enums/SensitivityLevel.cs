namespace GameGuild.Modules.Audit.Enums;

/// <summary>
/// Sensitivity levels for field-level data access auditing
/// </summary>
public enum SensitivityLevel
{
    /// <summary>
    /// Public data with no restrictions
    /// </summary>
    Public = 0,

    /// <summary>
    /// Internal data with limited access
    /// </summary>
    Internal = 1,

    /// <summary>
    /// Confidential data requiring authorization
    /// </summary>
    Confidential = 2,

    /// <summary>
    /// Restricted data with strict access controls
    /// </summary>
    Restricted = 3,

    /// <summary>
    /// Highly restricted data (PII, passwords, etc.)
    /// </summary>
    HighlyRestricted = 4
}
