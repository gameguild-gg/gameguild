using System.Collections.ObjectModel;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Simplified DTO for subscription usage information
/// </summary>
public abstract class SubscriptionUsageDto
{
    public Guid SubscriptionId { get; set; }

    public int UsersCount { get; set; }

    public int? MaxUsers { get; set; }

    public long StorageUsedMb { get; set; }

    public long? MaxStorageMb { get; set; }

    public long ApiCallsThisMonth { get; set; }

    public long? MaxApiCallsPerMonth { get; set; }

    public bool IsOverLimit { get; set; }

    public Collection<string> LimitWarnings { get; } = new Collection<string>();
}
