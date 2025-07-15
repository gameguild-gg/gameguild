namespace GameGuild.Modules.Products.Models;

/// <summary>
/// Access levels for products
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// Public access - anyone can view
    /// </summary>
    Public = 0,

    /// <summary>
    /// Members only access
    /// </summary>
    Members = 1,

    /// <summary>
    /// Premium access required
    /// </summary>
    Premium = 2,

    /// <summary>
    /// Admin access only
    /// </summary>
    Admin = 3,

    /// <summary>
    /// Private access - invite only
    /// </summary>
    Private = 4
}

/// <summary>
/// Product status enumeration
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is in draft state
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Product is published and available
    /// </summary>
    Published = 1,

    /// <summary>
    /// Product is archived and not available
    /// </summary>
    Archived = 2,

    /// <summary>
    /// Product is temporarily suspended
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Product is under review
    /// </summary>
    UnderReview = 4,

    /// <summary>
    /// Product is discontinued
    /// </summary>
    Discontinued = 5
}
