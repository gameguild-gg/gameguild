using GameGuild.Modules.DeveloperPortal.Entities;

namespace GameGuild.Modules.DeveloperPortal.Configuration;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.KeyHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(k => k.Scopes)
            .HasColumnType("jsonb");

        builder.HasIndex(k => k.DeveloperId);
        builder.HasIndex(k => k.TenantId);
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => k.IsActive);
        builder.HasIndex(k => k.IsRevoked);
    }
}

public class ApiUsageLogConfiguration : IEntityTypeConfiguration<ApiUsageLog>
{
    public void Configure(EntityTypeBuilder<ApiUsageLog> builder)
    {
        builder.ToTable("ApiUsageLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.Method)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(l => l.IpAddress)
            .HasMaxLength(45);

        builder.Property(l => l.UserAgent)
            .HasMaxLength(500);

        builder.HasIndex(l => l.ApiKeyId);
        builder.HasIndex(l => l.RequestedAt);
        builder.HasIndex(l => l.StatusCode);
    }
}

public class DeveloperOnboardingConfiguration : IEntityTypeConfiguration<DeveloperOnboarding>
{
    public void Configure(EntityTypeBuilder<DeveloperOnboarding> builder)
    {
        builder.ToTable("DeveloperOnboardings");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CurrentStep)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CompletedSteps)
            .HasColumnType("jsonb");

        builder.HasIndex(o => o.DeveloperId).IsUnique();
        builder.HasIndex(o => o.TenantId);
        builder.HasIndex(o => o.IsCompleted);
    }
}
