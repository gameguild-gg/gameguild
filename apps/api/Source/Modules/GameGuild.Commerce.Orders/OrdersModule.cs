using GameGuild.Commerce.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Orders module registration
/// </summary>
public static class OrdersModule
{
    /// <summary>
    /// Configure services for the Orders module
    /// </summary>
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register services
        services.AddScoped<IOrderService, OrderService>();

        // CQRS handlers are automatically registered by assembly scanning in ApplicationLayerExtensions

        return services;
    }

    /// <summary>
    /// Configure EF Core model for the Orders module
    /// </summary>
    public static void ConfigureOrdersModel(ModelBuilder modelBuilder)
    {
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
