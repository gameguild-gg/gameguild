namespace GameGuild;

/// <summary>
/// Represents the visibility level of content (who can see it).
/// Not to be confused with <c>GameGuild.Identity.Authorization.AccessLevel</c> which represents permission levels (None/Read/Write/Admin).
/// </summary>
public enum ContentVisibility
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