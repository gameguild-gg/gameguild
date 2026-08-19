using GameGuild.Notifications;
using GameGuild.Notifications.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Infrastructure;

internal sealed class SubscriptionsTestDbContext(DbContextOptions<SubscriptionsTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Notification> Notifications => Set<Notification>();

    Task<IDbContextTransaction> IApplicationDbContext.BeginTransactionAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("InMemory test context does not support transactions");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new NotificationConfiguration().Configure(modelBuilder.Entity<Notification>());
        base.OnModelCreating(modelBuilder);
    }
}
