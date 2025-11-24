namespace GameGuild.Tenants.Models;

/// <summary>
///     Represents a usage metric for validation
/// </summary>
public abstract record UsageMetric(string Name, decimal Current, decimal Limit, string Unit = "");
