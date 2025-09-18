using GameGuild.Database;
using GameGuild.Modules.Features.Infrastructure;
using GameGuild.Modules.Features.Models;
using GameGuild.Modules.Features.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFeature;

namespace GameGuild.Modules.Features.Tests;

/// <summary>
/// Integration test to verify the unified OpenFeature architecture with database provider
/// </summary>
public class FeatureFlagIntegrationTest {

    /// <summary>
    /// Test that demonstrates the complete flow:
    /// 1. DatabaseFeatureFlagProvider reads from database
    /// 2. FeatureFlagService uses OpenFeature API
    /// 3. Everything is properly integrated
    /// </summary>
    public async Task TestUnifiedOpenFeatureArchitecture() {
        // Arrange - Set up in-memory database
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
          options.UseInMemoryDatabase("TestDb"));

        services.AddLogging(builder => builder.AddConsole());

        // Register our integrated architecture
        services.AddSingleton<DatabaseFeatureFlagProvider>();
        services.AddSingleton(Api.Instance);
        services.AddSingleton<FeatureClient>(provider => {
            var api = provider.GetRequiredService<Api>();
            var databaseProvider = provider.GetRequiredService<DatabaseFeatureFlagProvider>();

            // Set the database provider as the default provider for OpenFeature
            api.SetProviderAsync(databaseProvider).GetAwaiter().GetResult();

            return api.GetClient();
        });
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        var serviceProvider = services.BuildServiceProvider();

        // Create test data
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var testFlag = new FeatureFlag {
            Key = "test-feature",
            Name = "Test Feature",
            Type = FeatureFlagType.Boolean,
            Value = "true",
            IsEnabled = true,
            Environment = "test"
        };

        dbContext.FeatureFlags.Add(testFlag);
        await dbContext.SaveChangesAsync();

        // Act - Test the unified service
        var featureFlagService = scope.ServiceProvider.GetRequiredService<IFeatureFlagService>();
        var context = new FeatureContext {
            Environment = "test"
        };

        var result = await featureFlagService.GetBooleanAsync("test-feature", false, context);

        // Assert - Should return true from database via OpenFeature
        if (!result) {
            throw new InvalidOperationException("Expected feature flag to return true from database");
        }

        Console.WriteLine("✅ Unified OpenFeature architecture test passed!");
        Console.WriteLine("   - DatabaseFeatureFlagProvider successfully reads from database");
        Console.WriteLine("   - FeatureFlagService properly uses OpenFeature API");
        Console.WriteLine("   - Integration works end-to-end");
    }
}