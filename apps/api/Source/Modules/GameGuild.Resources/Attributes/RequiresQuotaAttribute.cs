
namespace GameGuild.Resources;

/// <summary>
///     Attribute to mark commands that require resource quota validation.
///     When applied to a command, the ResourceQuotaBehavior will automatically
///     check and enforce quotas before executing the command.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequiresQuotaAttribute : Attribute
{
    /// <summary>
    ///     Creates a new RequiresQuota attribute
    /// </summary>
    /// <param name="resourceType">The type of resource that requires quota validation</param>
    /// <param name="amount">The amount of resource to consume (default: 1)</param>
    public RequiresQuotaAttribute(ResourceUsageType resourceType, long amount = 1)
    {
        ResourceType = resourceType;
        Amount = amount;
    }

    /// <summary>
    ///     The type of resource that requires quota validation
    /// </summary>
    public ResourceUsageType ResourceType { get; }

    /// <summary>
    ///     The amount of resource to consume
    /// </summary>
    public long Amount { get; }

    /// <summary>
    ///     Whether to record usage after successful command execution (default: true)
    /// </summary>
    public bool RecordUsage { get; init; } = true;

    /// <summary>
    ///     Optional source identifier for usage tracking
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    ///     Whether to throw exception on quota exceeded (true) or just log warning (false).
    ///     DEPRECATED: Hard limits should always be enforced. This option will be removed in a future version.
    ///     Setting to false is a security risk as it allows quota bypass.
    /// </summary>
    [Obsolete("EnforceHardLimit=false is deprecated and will be ignored. Hard limits are always enforced for security. This property will be removed in a future version.")]
    public bool EnforceHardLimit { get; init; } = true;
}
