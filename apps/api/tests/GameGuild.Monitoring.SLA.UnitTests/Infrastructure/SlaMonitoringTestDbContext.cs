using Microsoft.EntityFrameworkCore;

namespace GameGuild.Monitoring.SLA.UnitTests.Infrastructure;

internal sealed class SlaMonitoringTestDbContext(DbContextOptions<SlaMonitoringTestDbContext> options) : DbContext(options)
{
    public DbSet<ServiceLevelObjective> ServiceLevelObjectives => Set<ServiceLevelObjective>();

    public DbSet<ServiceLevelIndicator> ServiceLevelIndicators => Set<ServiceLevelIndicator>();

    public DbSet<SloViolation> SloViolations => Set<SloViolation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ServiceLevelObjectiveConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceLevelIndicatorConfiguration());
        modelBuilder.ApplyConfiguration(new SloViolationConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}