using GameGuild.Modules.Experiments.Entities;


namespace GameGuild.Modules.Experiments.Configuration;

public class PricingExperimentConfiguration : IEntityTypeConfiguration<PricingExperiment>
{
    public void Configure(EntityTypeBuilder<PricingExperiment> builder)
    {
        builder.ToTable("PricingExperiments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Status).IsRequired().HasConversion<string>();
        builder.Property(e => e.Type).IsRequired().HasConversion<string>();
        builder.Property(e => e.TargetSampleSize).IsRequired();
        builder.Property(e => e.ConfidenceLevel).IsRequired().HasPrecision(5, 4);
        builder.Property(e => e.SignificanceThreshold).IsRequired().HasPrecision(5, 4);
        builder.Property(e => e.Hypothesis).HasMaxLength(2000);
        builder.Property(e => e.Metadata).HasColumnType("jsonb");
        builder.Property(e => e.CreatedByUserId).IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Status, e.StartDate });

        builder.HasMany(e => e.Variants)
            .WithOne(v => v.Experiment)
            .HasForeignKey(v => v.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.UserAssignments)
            .WithOne(a => a.Experiment)
            .HasForeignKey(a => a.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExperimentVariantConfiguration : IEntityTypeConfiguration<ExperimentVariant>
{
    public void Configure(EntityTypeBuilder<ExperimentVariant> builder)
    {
        builder.ToTable("ExperimentVariants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ExperimentId).IsRequired();
        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Description).HasMaxLength(1000);
        builder.Property(v => v.IsControl).IsRequired();
        builder.Property(v => v.TrafficAllocation).IsRequired();
        builder.Property(v => v.PriceOverride).HasPrecision(18, 2);
        builder.Property(v => v.PricingConfiguration).HasColumnType("jsonb");
        builder.Property(v => v.FeatureFlags).HasColumnType("jsonb");
        builder.Property(v => v.ImpressionCount).IsRequired();
        builder.Property(v => v.ConversionCount).IsRequired();
        builder.Property(v => v.Revenue).IsRequired().HasPrecision(18, 2);

        builder.Ignore(v => v.ConversionRate);
        builder.Ignore(v => v.AverageRevenuePerUser);

        builder.HasIndex(v => v.ExperimentId);
        builder.HasIndex(v => new { v.ExperimentId, v.IsControl });

        builder.HasMany(v => v.UserAssignments)
            .WithOne(a => a.Variant)
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Results)
            .WithOne(r => r.Variant)
            .HasForeignKey(r => r.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserAssignmentConfiguration : IEntityTypeConfiguration<UserAssignment>
{
    public void Configure(EntityTypeBuilder<UserAssignment> builder)
    {
        builder.ToTable("UserAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ExperimentId).IsRequired();
        builder.Property(a => a.VariantId).IsRequired();
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.AssignedAt).IsRequired();
        builder.Property(a => a.HasConverted).IsRequired();
        builder.Property(a => a.ConversionRevenue).HasPrecision(18, 2);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Metadata).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.UserId, a.ExperimentId }).IsUnique();
        builder.HasIndex(a => a.ExperimentId);
        builder.HasIndex(a => a.VariantId);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.ExperimentId, a.HasConverted });
    }
}

public class ExperimentResultConfiguration : IEntityTypeConfiguration<ExperimentResult>
{
    public void Configure(EntityTypeBuilder<ExperimentResult> builder)
    {
        builder.ToTable("ExperimentResults");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ExperimentId).IsRequired();
        builder.Property(r => r.VariantId).IsRequired();
        builder.Property(r => r.CalculatedAt).IsRequired();
        builder.Property(r => r.SampleSize).IsRequired();
        builder.Property(r => r.ConversionRate).IsRequired().HasPrecision(18, 6);
        builder.Property(r => r.ConfidenceLevel).IsRequired().HasPrecision(5, 4);
        builder.Property(r => r.PValue).IsRequired().HasPrecision(18, 6);
        builder.Property(r => r.ZScore).IsRequired().HasPrecision(18, 6);
        builder.Property(r => r.IsStatisticallySignificant).IsRequired();
        builder.Property(r => r.TotalRevenue).IsRequired().HasPrecision(18, 2);
        builder.Property(r => r.AverageRevenuePerUser).IsRequired().HasPrecision(18, 2);
        builder.Property(r => r.StandardError).IsRequired().HasPrecision(18, 6);
        builder.Property(r => r.LowerBound).IsRequired().HasPrecision(18, 6);
        builder.Property(r => r.UpperBound).IsRequired().HasPrecision(18, 6);
        builder.Property(r => r.Lift).HasPrecision(18, 6);

        builder.HasIndex(r => r.ExperimentId);
        builder.HasIndex(r => r.VariantId);
        builder.HasIndex(r => new { r.ExperimentId, r.CalculatedAt });

        builder.HasOne(r => r.Experiment)
            .WithMany()
            .HasForeignKey(r => r.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
