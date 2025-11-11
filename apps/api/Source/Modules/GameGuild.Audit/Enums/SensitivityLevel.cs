namespace GameGuild.Modules.Audit.Enums;

/// <summary>
/// Defines sensitivity levels for data access auditing
/// </summary>
public enum SensitivityLevel
{
    /// <summary>Public data with no sensitivity</summary>
    Public = 0,

    /// <summary>Internal data for organization use</summary>
    Internal = 1,

    /// <summary>Confidential data with restricted access</summary>
    Confidential = 2,

    /// <summary>Restricted data with limited access</summary>
    Restricted = 3,

    /// <summary>Highly restricted data with minimal access</summary>
    HighlyRestricted = 4
}