namespace GameGuild.SharedKernel.Enums;

/// <summary>
/// Represents the publication status of content
/// </summary>
public enum ContentStatus
{
    /// <summary>Content is in draft state</summary>
    Draft = 0,
    
    /// <summary>Content is under review</summary>
    Review = 1,
    
    /// <summary>Content is published and visible</summary>
    Published = 2,
    
    /// <summary>Content is archived and not visible</summary>
    Archived = 3,
    
    /// <summary>Content is deleted</summary>
    Deleted = 4
}