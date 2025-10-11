using GameGuild.Modules.Products.Application.Features.GetProduct;
using GameGuild.Modules.Products.Application.Features.ManageProduct;
using GameGuild.Modules.Products.Domain.Entities;
using GameGuild.Modules.Products.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products;

/// <summary>
/// Products module registration
/// </summary>
public static class ProductsModule
{
    /// <summary>
    /// Configure services for the Products module
    /// </summary>
    public static IServiceCollection AddProductsModule(this IServiceCollection services)
    {
        // Register CQRS handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProductCommandHandlers).Assembly));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProductQueryHandlers).Assembly));

        return services;
    }

    /// <summary>
    /// Configure EF Core model for the Products module
    /// </summary>
    public static void ConfigureProductsModel(ModelBuilder modelBuilder)
    {
        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductPricingConfiguration());
        modelBuilder.ApplyConfiguration(new PromoCodeConfiguration());
        modelBuilder.ApplyConfiguration(new PromoCodeUseConfiguration());
        modelBuilder.ApplyConfiguration(new UserProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductSubscriptionPlanConfiguration());

        // Configure DbSets
        modelBuilder.Entity<Product>();
        modelBuilder.Entity<ProductPricing>();
        modelBuilder.Entity<PromoCode>();
        modelBuilder.Entity<PromoCodeUse>();
        modelBuilder.Entity<UserProduct>();
        modelBuilder.Entity<ProductSubscriptionPlan>();
    }
}