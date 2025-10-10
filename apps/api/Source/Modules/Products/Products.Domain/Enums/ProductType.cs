namespace GameGuild.Modules.Products.Domain.Enums;

/// <summary>
/// Product type enumeration
/// </summary>
public enum ProductType
{
    /// <summary>Course product type</summary>
    Course = 0,

    /// <summary>Program product type (collection of courses)</summary>
    Program = 1,

    /// <summary>Digital content/resource</summary>
    Resource = 2,

    /// <summary>Subscription-based access</summary>
    Subscription = 3,

    /// <summary>Physical product</summary>
    Physical = 4,

    /// <summary>Service or consulting</summary>
    Service = 5
}