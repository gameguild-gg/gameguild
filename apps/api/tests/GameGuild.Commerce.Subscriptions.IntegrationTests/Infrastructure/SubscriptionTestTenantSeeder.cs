using GameGuild.API.Database;
using GameGuild.Identity.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests.Infrastructure;

internal static class SubscriptionTestTenantSeeder
{
    public static void EnsureTenantExists(IServiceProvider services, Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            return;
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (dbContext.Set<Tenant>().Any(tenant => tenant.Id == tenantId))
        {
            return;
        }

        dbContext.Set<Tenant>().Add(new Tenant
        {
            Id = tenantId,
            Name = $"Test Tenant {tenantId:N}",
            Slug = $"test-tenant-{tenantId:N}",
            AdminEmail = $"tenant-{tenantId:N}@example.test",
            IsActive = true
        });

        dbContext.SaveChanges();
    }
}
