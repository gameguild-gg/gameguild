namespace GameGuild.SharedKernel.Enums;

/// <summary>
/// Represents the access level/visibility of content
/// </summary>
public enum AccessLevel
{
    /// <summary>Private - only accessible by owner</summary>
    Private = 0,
    
    /// <summary>Internal - accessible by organization members</summary>
    Internal = 1,
    
    /// <summary>Friends - accessible by friends/connections</summary>
    Friends = 2,
    
    /// <summary>Protected - accessible with specific permissions</summary>
    Protected = 3,
    
    /// <summary>Public - accessible by everyone</summary>
    Public = 4
}