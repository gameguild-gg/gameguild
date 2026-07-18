namespace GameGuild.Commerce.Orders;

/// <summary>
/// Identifies the audited order actions that production composition may expose.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class MinimumOrderRouteAttribute : Attribute;
