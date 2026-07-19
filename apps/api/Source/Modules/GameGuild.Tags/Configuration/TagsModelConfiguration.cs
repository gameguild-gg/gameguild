using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tags;

public sealed class TagsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>();
        modelBuilder.Entity<TagProficiency>();
        modelBuilder.Entity<CertificateTag>();
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TagsModelConfiguration).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Tags", StringComparison.Ordinal) == true);
    }
}
