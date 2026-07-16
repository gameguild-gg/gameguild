using GameGuild.Commerce.Products;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GameGuild.Projects.UnitTests.Channels;

public sealed class ProjectStoreProductModelTests
{
    [Fact]
    public void Bridge_Should_Contain_Only_Identity_And_Audit_Data()
    {
        typeof(ProjectStoreProduct).GetProperty(nameof(ProjectStoreProduct.ProjectId)).Should().NotBeNull();
        typeof(ProjectStoreProduct).GetProperty(nameof(ProjectStoreProduct.ProductId)).Should().NotBeNull();
        typeof(ProjectStoreProduct).GetProperty("ProjectName").Should().BeNull();
        typeof(ProjectStoreProduct).GetProperty("ProjectStatus").Should().BeNull();
        typeof(ProjectStoreProduct).GetProperty("ProjectImageUrl").Should().BeNull();
    }

    [Fact]
    public void Configure_Should_Map_Filtered_Unique_Pair_And_Required_Foreign_Keys()
    {
        var modelBuilder = new ModelBuilder();
        new ProjectsModelConfiguration().Configure(modelBuilder);

        var entity = modelBuilder.Model.FindEntityType(typeof(ProjectStoreProduct))!;
        entity.GetTableName().Should().Be("project_store_products");
        entity.GetIndexes().Should().ContainSingle(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(ProjectStoreProduct.ProjectId), nameof(ProjectStoreProduct.ProductId) }) &&
            index.GetFilter() == "\"DeletedAt\" IS NULL");

        entity.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Project) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict &&
            foreignKey.IsRequired);
        entity.GetForeignKeys().Should().Contain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Product) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Cascade &&
            foreignKey.IsRequired);
        entity.GetProperties().Should().NotContain(property => property.IsShadowProperty() && property.Name.StartsWith("ProjectId", StringComparison.Ordinal));
    }

    [Fact]
    public void Projects_Should_Own_The_Only_Project_Product_Dependency()
    {
        typeof(Project).Assembly.GetReferencedAssemblies().Select(name => name.Name)
            .Should().Contain("GameGuild.Commerce.Products");
        typeof(Product).Assembly.GetReferencedAssemblies().Select(name => name.Name)
            .Should().NotContain("GameGuild.Projects");
    }
}
