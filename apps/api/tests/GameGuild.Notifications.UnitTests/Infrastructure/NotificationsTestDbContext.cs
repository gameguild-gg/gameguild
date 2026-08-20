namespace GameGuild.Notifications.UnitTests.Infrastructure;

internal sealed class NotificationsTestDbContext(DbContextOptions<NotificationsTestDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    public DbSet<EmailDeliveryEvent> EmailDeliveryEvents => Set<EmailDeliveryEvent>();

    public DbSet<EmailSuppression> EmailSuppressions => Set<EmailSuppression>();

    // BeginTransactionAsync is a Relational facade extension, not a DbContext member, so it needs an explicit impl.
    Task<IDbContextTransaction> IApplicationDbContext.BeginTransactionAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("InMemory test context does not support transactions");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new NotificationConfiguration().Configure(modelBuilder.Entity<Notification>());
        new NotificationTemplateConfiguration().Configure(modelBuilder.Entity<NotificationTemplate>());
        new NotificationPreferenceConfiguration().Configure(modelBuilder.Entity<NotificationPreference>());
        new EmailDeliveryEventConfiguration().Configure(modelBuilder.Entity<EmailDeliveryEvent>());
        new EmailSuppressionConfiguration().Configure(modelBuilder.Entity<EmailSuppression>());
        base.OnModelCreating(modelBuilder);
    }
}
