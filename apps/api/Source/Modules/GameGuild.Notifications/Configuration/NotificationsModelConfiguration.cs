using Microsoft.EntityFrameworkCore;

namespace GameGuild.Notifications.Configuration;

/// <summary>
/// Registers the Notifications module entities with the shared application model.
/// </summary>
public sealed class NotificationsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Notification).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Notifications", StringComparison.Ordinal) == true);
    }
}
