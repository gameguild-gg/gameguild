using Microsoft.EntityFrameworkCore;

namespace GameGuild.Monitoring.SLA;

public sealed class SlaModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ServiceLevelIndicatorConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceLevelObjectiveConfiguration());
        modelBuilder.ApplyConfiguration(new SloViolationConfiguration());
    }
}
