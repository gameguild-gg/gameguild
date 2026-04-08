using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Consent;

public class ConsentModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ConsentModelConfiguration).Assembly);
    }
}
