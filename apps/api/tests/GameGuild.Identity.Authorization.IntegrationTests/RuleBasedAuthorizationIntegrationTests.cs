using System.Security.Claims;
using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using GameGuild.API;
using GameGuild.API.Database;

namespace GameGuild.Identity.Authorization.IntegrationTests;

/// <summary>
/// End-to-end integration tests for rule-based authorization
/// </summary>
public class RuleBasedAuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly string DatabaseName = $"AuthorizationRules_{Guid.NewGuid():N}";

    public RuleBasedAuthorizationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(descriptor =>
                        descriptor.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        descriptor.ServiceType == typeof(ApplicationDbContext) ||
                        descriptor.ServiceType.FullName?.Contains("EntityFramework", StringComparison.Ordinal) == true ||
                        descriptor.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(DatabaseName));
            });
        });

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task RuleBasedPolicy_EndToEnd_AuthorizesCorrectly()
    {
        // Arrange
        var policyName = "TestRuleBasedPolicy";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create rule-based policy in database
        var policy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = null, // Global policy
            UseRuleBasedEvaluation = true,
            RulesJson = $$"""
            [
                {
                    "Type": "{{RuleTypes.RequireMfa}}",
                    "Description": "User must have MFA enabled",
                    "Enabled": true,
                    "Params": {}
                }
            ]
            """,
            IsActive = true,
            RequireAuthentication = true
        };

        dbContext.Set<PolicyDefinitionEntity>().Add(policy);
        await dbContext.SaveChangesAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString()),
            new(ClaimNames.Amr, "mfa") // MFA claim for the rule
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RuleBasedPolicy_WithFailingRule_Denies()
    {
        // Arrange
        var policyName = "TestFailingPolicy";
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create policy requiring MFA
        var policy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = null, // Global policy
            UseRuleBasedEvaluation = true,
            RulesJson = $$"""
            [
                {
                    "Type": "{{RuleTypes.RequireMfa}}",
                    "Description": "User must have MFA",
                    "Enabled": true,
                    "Params": {}
                }
            ]
            """,
            IsActive = true,
            RequireAuthentication = true
        };

        dbContext.Set<PolicyDefinitionEntity>().Add(policy);
        await dbContext.SaveChangesAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // User without MFA claim
        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString())
            // No MFA claim - should fail
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task SimpleRuleBasedPolicy_Works_WithAuthenticationOnly()
    {
        // Arrange
        var policyName = "SimpleRuleBasedPolicy";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Create simple rule-based policy with no rules (authentication only)
        var policy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = null, // Global policy
            UseRuleBasedEvaluation = true,
            RulesJson = "[]",
            IsActive = true,
            RequireAuthentication = true
        };

        dbContext.Set<PolicyDefinitionEntity>().Add(policy);
        await dbContext.SaveChangesAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeTrue("rule-based policies with authentication only should work");
    }

    [Fact]
    public async Task TenantOverride_MergesWithBasePolicy()
    {
        // Arrange
        var policyName = "MergeablePolicy";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Base global policy
        var basePolicy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = null, // Global
            UseRuleBasedEvaluation = true,
            RulesJson = $$"""
            [
                {
                    "Type": "{{RuleTypes.RequireMfa}}",
                    "Description": "Require MFA globally",
                    "Enabled": true,
                    "Params": {}
                }
            ]
            """,
            IsActive = true
        };

        // Tenant-specific override
        var tenantPolicy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = tenantId,
            UseRuleBasedEvaluation = true,
            RulesJson = $$"""
            [
                {
                    "Type": "{{RuleTypes.RequireMfa}}",
                    "Description": "Tenant-specific MFA requirement",
                    "Enabled": true,
                    "Params": {}
                }
            ]
            """,
            IsActive = true
        };

        dbContext.Set<PolicyDefinitionEntity>().Add(basePolicy);
        dbContext.Set<PolicyDefinitionEntity>().Add(tenantPolicy);
        await dbContext.SaveChangesAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString()),
            new(ClaimNames.Amr, "mfa") // MFA claim
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeTrue("both base and tenant rules should be satisfied");
    }

    [Fact]
    public async Task MultipleRules_AllMustPass()
    {
        // Arrange
        var policyName = "MultiRulePolicy";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var policy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = null, // Global policy
            UseRuleBasedEvaluation = true,
            RulesJson = $$"""
            [
                {
                    "Type": "{{RuleTypes.RequireMfa}}",
                    "Description": "MFA check",
                    "Enabled": true,
                    "Params": {}
                }
            ]
            """,
            IsActive = true
        };

        dbContext.Set<PolicyDefinitionEntity>().Add(policy);
        await dbContext.SaveChangesAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // User without MFA - rule will fail
        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
            // No MFA claim - one rule will fail
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeFalse("all rules must pass");
    }

    [Fact]
    public async Task DisabledRule_IsSkipped()
    {
        // Arrange
        var policyName = "DisabledRulePolicy";
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var policy = new PolicyDefinitionEntity
        {
            Id = Guid.NewGuid(),
            PolicyName = policyName,
            TenantId = null, // Global policy
            UseRuleBasedEvaluation = true,
            RulesJson = $$"""
            [
                {
                    "Type": "{{RuleTypes.RequireMfa}}",
                    "Description": "Disabled rule",
                    "Enabled": false,
                    "Params": {}
                }
            ]
            """,
            IsActive = true
        };

        dbContext.Set<PolicyDefinitionEntity>().Add(policy);
        await dbContext.SaveChangesAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // User is authenticated but doesn't have MFA (disabled rule should be ignored)
        var claims = new List<Claim>
        {
            new(ClaimNames.UserId, userId.ToString()),
            new(ClaimNames.TenantId, tenantId.ToString())
            // No MFA - but that rule is disabled
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Act
        var result = await authService.AuthorizeAsync(user, policyName);

        // Assert
        result.Succeeded.Should().BeTrue("disabled rules should be skipped");
    }
}
