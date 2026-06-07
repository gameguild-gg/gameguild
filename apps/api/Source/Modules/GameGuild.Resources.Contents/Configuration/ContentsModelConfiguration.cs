using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources.Contents.Configuration;

public sealed class ContentsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ContentsModelConfiguration).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Resources.Contents", StringComparison.Ordinal) == true);
    }
}
