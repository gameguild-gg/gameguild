using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Commerce.Products;

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
        // Register repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductPricingRepository, ProductPricingRepository>();
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<IUserProductRepository, UserProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register services
        services.AddScoped<IPricingEngineService, PricingEngineService>();
        services.AddScoped<IProductPricingService, ProductPricingService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();
        services.AddScoped<IUserProductService, UserProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IEntitlementService, EntitlementService>();

        // CQRS handlers are automatically registered by assembly scanning in ApplicationLayerExtensions

        return services;
    }

    /// <summary>
    /// Configure EF Core model for the Products module
    /// </summary>
    public static void ConfigureProductsModel(ModelBuilder modelBuilder)
    {
        // Configure DbSets with fluent API
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatorId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.ReferralCommissionPercentage).HasPrecision(5, 2);
            entity.Property(e => e.MaxAffiliateDiscount).HasPrecision(5, 2);
            entity.Property(e => e.AffiliateCommissionPercentage).HasPrecision(5, 2);

            entity.HasMany(e => e.Pricing)
                .WithOne(p => p.Product)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UserProducts)
                .WithOne(up => up.Product)
                .HasForeignKey(up => up.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductPricing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.IsDefault);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BasePrice).HasPrecision(10, 2);
            entity.Property(e => e.SalePrice).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
        });

        modelBuilder.Entity<UserProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.AccessStatus);
            entity.Property(e => e.PricePaid).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
        });

        modelBuilder.Entity<PromoCodeUse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PromoCodeId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.DiscountApplied).HasPrecision(10, 2);
        });

        modelBuilder.Entity<ProductSubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
        });

        modelBuilder.Entity<PricingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.RuleType);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<PricingTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
        });

        modelBuilder.Entity<PromoStackingRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.MaxTotalDiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.MaxTotalDiscountAmount).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subtotal).HasPrecision(10, 2);
            entity.Property(e => e.DiscountTotal).HasPrecision(10, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(10, 2);
            entity.Property(e => e.Total).HasPrecision(10, 2);
            entity.Property(e => e.RefundAmount).HasPrecision(10, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.PaymentProviderReference).HasMaxLength(200);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RefundReason).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.HasMany(e => e.LineItems)
                .WithOne(li => li.Order)
                .HasForeignKey(li => li.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLineItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.Property(e => e.ProductNameSnapshot).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPriceSnapshot).HasPrecision(10, 2);
            entity.Property(e => e.BasePriceSnapshot).HasPrecision(10, 2);
            entity.Property(e => e.SalePriceSnapshot).HasPrecision(10, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.LineTotal).HasPrecision(10, 2);
            entity.Property(e => e.PromoCodesApplied).HasMaxLength(500);
            entity.Property(e => e.PricingTierNameSnapshot).HasMaxLength(100);
            entity.Property(e => e.BillingIntervalSnapshot).HasMaxLength(20);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UserProduct)
                .WithMany()
                .HasForeignKey(e => e.UserProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
