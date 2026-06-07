using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.FERPA;

public sealed class FerpaModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FerpaModelConfiguration).Assembly);
    }
}
