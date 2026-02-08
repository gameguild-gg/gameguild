using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
///     EF Core model configuration for the Orders module.
///     Delegates to the existing <see cref="OrdersModule.ConfigureOrdersModel"/> method
///     which uses inline fluent API for Order, OrderLineItem, and OrderAuditLog.
/// </summary>
public sealed class OrdersModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        OrdersModule.ConfigureOrdersModel(modelBuilder);
    }
}
