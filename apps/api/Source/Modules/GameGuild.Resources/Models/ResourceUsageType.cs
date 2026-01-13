namespace GameGuild.Resources;

/// <summary>
///     Types of resource usage that can be tracked and limited.
///     Each type represents a quota-controlled resource in the system.
/// </summary>
public enum ResourceUsageType
{
    /// <summary>User accounts per tenant</summary>
    Users = 1,

    /// <summary>Projects created per tenant</summary>
    Projects = 2,

    /// <summary>Storage usage in bytes</summary>
    Storage = 3,

    /// <summary>API calls per period</summary>
    ApiCalls = 4,

    /// <summary>Programs (learning paths) per tenant</summary>
    Programs = 5,

    /// <summary>Courses per tenant</summary>
    Courses = 6,

    /// <summary>Feature flags per tenant</summary>
    FeatureFlags = 7,

    /// <summary>Subscription plans per tenant</summary>
    SubscriptionPlans = 8,

    /// <summary>Products in catalog per tenant</summary>
    Products = 9,

    /// <summary>Testing sessions per tenant</summary>
    TestingSessions = 10,

    /// <summary>Roles per tenant</summary>
    Roles = 11
}
