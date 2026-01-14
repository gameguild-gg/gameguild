using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Products;

/// <summary>
///     EF Core configuration for Product entity
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        // Configure the relationship for products that ARE bundles
        // BundleItems = products included IN this bundle
        builder.HasMany(p => p.BundleItems)
            .WithOne(bi => bi.BundleProduct)
            .HasForeignKey(bi => bi.BundleProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure the inverse relationship
        // IncludedInBundles = bundles that include this product
        builder.HasMany(p => p.IncludedInBundles)
            .WithOne(bi => bi.IncludedProduct)
            .HasForeignKey(bi => bi.IncludedProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
///     EF Core configuration for ProductBundleItem entity
/// </summary>
public class ProductBundleItemConfiguration : IEntityTypeConfiguration<ProductBundleItem>
{
    public void Configure(EntityTypeBuilder<ProductBundleItem> builder)
    {
        builder.HasKey(x => x.Id);

        // Composite unique index for bundle-included product pairs
        builder.HasIndex(x => new { x.BundleProductId, x.IncludedProductId }).IsUnique();
    }
}
