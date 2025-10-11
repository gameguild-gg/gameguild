using GameGuild.Modules.Products.Domain.Entities;
using ProductEntity = GameGuild.Modules.Products.Domain.Entities.Product;

namespace GameGuild.Modules.Products.Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for Product entity
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasMaxLength(4000);

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.BundleItems)
            .HasMaxLength(4000);

        builder.Property(p => p.ReferralCommissionPercentage)
            .HasPrecision(5, 2);

        builder.Property(p => p.MaxAffiliateDiscount)
            .HasPrecision(18, 2);

        builder.Property(p => p.AffiliateCommissionPercentage)
            .HasPrecision(5, 2);

        // Indexes
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Type);
        builder.HasIndex(p => p.CreatorId);
        builder.HasIndex(p => p.IsBundle);

        // Navigation properties
        builder.HasMany(p => p.Pricing)
            .WithOne()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.UserProducts)
            .WithOne(up => up.Product)
            .HasForeignKey(up => up.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProductSubscriptionPlans)
            .WithOne()
            .HasForeignKey(psp => psp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration for ProductPricing entity
/// </summary>
public class ProductPricingConfiguration : IEntityTypeConfiguration<ProductPricing>
{
    public void Configure(EntityTypeBuilder<ProductPricing> builder)
    {
        builder.ToTable("ProductPricing");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.BasePrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(pp => pp.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(pp => pp.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        // Indexes
        builder.HasIndex(pp => new { pp.ProductId, pp.Currency, pp.IsDefault });
        builder.HasIndex(pp => new { pp.ProductId, pp.IsDefault });
    }
}

/// <summary>
/// EF Core configuration for PromoCode entity
/// </summary>
public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");

        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pc => pc.Description)
            .HasMaxLength(500);

        builder.Property(pc => pc.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(pc => pc.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(pc => pc.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(pc => pc.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        // Unique constraint on Code
        builder.HasIndex(pc => pc.Code)
            .IsUnique();

        builder.HasIndex(pc => pc.Type);
        builder.HasIndex(pc => pc.IsActive);
        builder.HasIndex(pc => pc.ExpiryDate);

        // Navigation properties
        builder.HasMany(pc => pc.PromoCodeUses)
            .WithOne(pcu => pcu.PromoCode)
            .HasForeignKey(pcu => pcu.PromoCodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF Core configuration for PromoCodeUse entity
/// </summary>
public class PromoCodeUseConfiguration : IEntityTypeConfiguration<PromoCodeUse>
{
    public void Configure(EntityTypeBuilder<PromoCodeUse> builder)
    {
        builder.ToTable("PromoCodeUses");

        builder.HasKey(pcu => pcu.Id);

        builder.Property(pcu => pcu.OriginalPrice)
            .HasPrecision(18, 2);

        builder.Property(pcu => pcu.DiscountApplied)
            .HasPrecision(18, 2);

        builder.Property(pcu => pcu.FinalPrice)
            .HasPrecision(18, 2);

        builder.Property(pcu => pcu.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        // Indexes
        builder.HasIndex(pcu => new { pcu.PromoCodeId, pcu.UserId });
        builder.HasIndex(pcu => pcu.UserId);
        builder.HasIndex(pcu => pcu.ProductId);
        builder.HasIndex(pcu => pcu.UsedAt);
    }
}

/// <summary>
/// EF Core configuration for UserProduct entity
/// </summary>
public class UserProductConfiguration : IEntityTypeConfiguration<UserProduct>
{
    public void Configure(EntityTypeBuilder<UserProduct> builder)
    {
        builder.ToTable("UserProducts");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.AccessStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(up => up.AcquisitionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(up => up.PricePaid)
            .HasPrecision(18, 2);

        builder.Property(up => up.Currency)
            .HasMaxLength(3);

        // Indexes
        builder.HasIndex(up => new { up.UserId, up.ProductId })
            .IsUnique();
        builder.HasIndex(up => up.UserId);
        builder.HasIndex(up => up.ProductId);
        builder.HasIndex(up => up.AccessStatus);
        builder.HasIndex(up => up.AcquisitionType);
        builder.HasIndex(up => up.AcquiredAt);
    }
}

/// <summary>
/// EF Core configuration for ProductSubscriptionPlan entity
/// </summary>
public class ProductSubscriptionPlanConfiguration : IEntityTypeConfiguration<ProductSubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<ProductSubscriptionPlan> builder)
    {
        builder.ToTable("ProductSubscriptionPlans");

        builder.HasKey(psp => psp.Id);

        builder.Property(psp => psp.BillingInterval)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(psp => new { psp.ProductId, psp.SubscriptionPlanId })
            .IsUnique();
        builder.HasIndex(psp => psp.ProductId);
        builder.HasIndex(psp => psp.SubscriptionPlanId);
        builder.HasIndex(psp => psp.IsActive);
        builder.HasIndex(psp => psp.IsDefault);
    }
}
