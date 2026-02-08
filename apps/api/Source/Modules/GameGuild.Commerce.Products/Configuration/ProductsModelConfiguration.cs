using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
///     EF Core model configuration for the Products module.
///     Applies <see cref="IEntityTypeConfiguration{TEntity}"/> classes from the assembly
///     plus inline fluent API configurations from <see cref="ProductsModule.ConfigureProductsModel"/>.
/// </summary>
public sealed class ProductsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        // Apply IEntityTypeConfiguration classes (ProductConfiguration, ProductBundleItemConfiguration)
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Product).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Commerce.Products", StringComparison.Ordinal) == true);

        // Apply inline fluent API configurations (indexes, precision, relationships for all other entities)
        ProductsModule.ConfigureProductsModel(modelBuilder);
    }
}
