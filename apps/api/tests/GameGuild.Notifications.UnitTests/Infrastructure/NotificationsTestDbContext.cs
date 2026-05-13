namespace GameGuild.Notifications.UnitTests.Infrastructure;

internal sealed class NotificationsTestDbContext(DbContextOptions<NotificationsTestDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new NotificationConfiguration().Configure(modelBuilder.Entity<Notification>());
        new NotificationTemplateConfiguration().Configure(modelBuilder.Entity<NotificationTemplate>());
        new NotificationPreferenceConfiguration().Configure(modelBuilder.Entity<NotificationPreference>());
        base.OnModelCreating(modelBuilder);
    }
}
