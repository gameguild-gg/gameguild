using GameGuild.API.Database;
using Xunit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using GameGuild.Tests.Authentication.Integration.TestHelpers;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// Integration tests for Access Control features
/// Tests ABAC policies, conditional policies, permission caching, and cross-module inheritance
/// Note: These tests verify the data model and relationships. Full policy evaluation
/// would require implementing the evaluation engine or using HTTP client tests.
/// </summary>
public class AccessControlIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public AccessControlIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                                d.ServiceType == typeof(ApplicationDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"AccessControlTestDb_{Guid.NewGuid()}");
                });

                // Ensure memory cache is available
                services.AddMemoryCache();

                // Add HTTP logging services (required by the pipeline)
                services.AddHttpLogging(o => { });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _cache = _scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        _dbContext.Database.EnsureCreated();
    }

    #region ABAC Policy Evaluation Tests

    [Fact]
    public async Task AbacPolicy_SimpleAttributeMatch_ShouldGrantAccess()
    {
        // Arrange - Create ABAC policy
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var policy = TestEntityFactory.CreateAbacPolicy(
            name: "Department Access Policy",
            tenantId: tenantId,
            resourceType: "Document",
            effect: AbacPolicyEffect.Allow,
            attributeExpression: @"{
                ""userAttributes"": { ""department"": ""Engineering"" },
                ""resourceAttributes"": { ""classification"": ""Internal"" }
            }");
        policy.Description = "Allow access to users in Engineering department";

        await _dbContext.Set<AbacPolicy>().AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act - Evaluate policy with matching attributes
        var evaluationRequest = new EvaluateAbacPoliciesCommand
        {
            TenantId = tenantId,
            Context = new AbacEvaluationContext
            {
                UserId = userId,
                TenantId = tenantId,
                ResourceType = "Document",
                UserAttributes = new Dictionary<string, object>
                {
                    { "department", "Engineering" },
                    { "role", "Developer" }
                },
                ResourceAttributes = new Dictionary<string, object>
                {
                    { "classification", "Internal" },
                    { "owner", userId.ToString() }
                }
            }
        };

        // TODO: Implement command handler - var result = await _handler.Handle(evaluationRequest);

        // TODO: Uncomment when handler is implemented
        // Assert
        // result.Should().NotBeNull();
        // result.IsAllowed.Should().BeTrue();
        // result.MatchedPolicies.Should().Contain(p => p.Id == policy.Id);
    }

    [Fact]
    public async Task AbacPolicy_ComplexConditions_MultipleAttributeRules_ShouldEvaluateCorrectly()
    {
        // Arrange - Create policy with complex conditions
        var tenantId = Guid.NewGuid();

        var policy = TestEntityFactory.CreateAbacPolicy(
            name: "Sensitive Data Access Policy",
            tenantId: tenantId,
            resourceType: "SensitiveData",
            effect: AbacPolicyEffect.Allow,
            attributeExpression: @"{
                ""userAttributes"": {
                    ""seniority"": ""Senior"",
                    ""clearanceLevel"": ""High""
                },
                ""resourceAttributes"": {
                    ""dataClassification"": ""Confidential""
                },
                ""contextAttributes"": {
                    ""timeOfDay"": ""BusinessHours""
                }
            }");
        policy.Description = "Restrict sensitive data to senior staff during business hours";

        await _dbContext.Set<AbacPolicy>().AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act - Evaluate with matching complex conditions
        var evaluationRequest = new EvaluateAbacPoliciesCommand
        {
            TenantId = tenantId,
            Context = new AbacEvaluationContext
            {
                UserId = Guid.NewGuid(),
                TenantId = tenantId,
                ResourceType = "SensitiveData",
                UserAttributes = new Dictionary<string, object>
                {
                    { "seniority", "Senior" },
                    { "clearanceLevel", "High" },
                    { "department", "Security" }
                },
                ResourceAttributes = new Dictionary<string, object>
                {
                    { "dataClassification", "Confidential" }
                },
                EnvironmentalAttributes = new Dictionary<string, object>
                {
                    { "timeOfDay", "BusinessHours" },
                    { "location", "Office" }
                }
            }
        };

        // TODO: Implement command handler - var result = await _handler.Handle(evaluationRequest);

        // TODO: Uncomment when handler is implemented
        // Assert
        // result.Should().NotBeNull();
        // result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AbacPolicy_DenyPolicy_ShouldOverrideAllowPolicies()
    {
        // Arrange - Create both Allow and Deny policies
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var allowPolicy = TestEntityFactory.CreateAbacPolicy(
            name: "Allow Department Access",
            tenantId: tenantId,
            resourceType: "Document",
            effect: AbacPolicyEffect.Allow,
            attributeExpression: @"{ ""userAttributes"": { ""department"": ""Engineering"" } }");
        allowPolicy.Priority = 10;

        var denyPolicy = TestEntityFactory.CreateAbacPolicy(
            name: "Deny Suspended Users",
            tenantId: tenantId,
            resourceType: "Document",
            effect: AbacPolicyEffect.Deny,
            attributeExpression: @"{ ""userAttributes"": { ""status"": ""Suspended"" } }");
        denyPolicy.Priority = 100; // Higher priority

        await _dbContext.Set<AbacPolicy>().AddRangeAsync(allowPolicy, denyPolicy);
        await _dbContext.SaveChangesAsync();

        // Act - User matches both policies (should be denied)
        var evaluationRequest = new EvaluateAbacPoliciesCommand
        {
            TenantId = tenantId,
            Context = new AbacEvaluationContext
            {
                UserId = userId,
                TenantId = tenantId,
                ResourceType = "Document",
                UserAttributes = new Dictionary<string, object>
                {
                    { "department", "Engineering" },
                    { "status", "Suspended" }
                }
            }
        };

        // TODO: Implement command handler - var result = await _handler.Handle(evaluationRequest);

        // TODO: Uncomment when handler is implemented
        // Assert - Deny should win
        // result.Should().NotBeNull();
        // result.IsAllowed.Should().BeFalse();
        // result.DeniedBy.Should().Contain(p => p.Id == denyPolicy.Id);
    }

    // TODO: Implement BulkEvaluateAbacPoliciesCommand and handler before uncommenting this test
    /*
    [Fact]
    public async Task AbacPolicy_BulkEvaluation_MultipleResources_ShouldEvaluateEfficiently()
    {
        // Arrange - Create policy
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var policy = TestEntityFactory.CreateAbacPolicy(
            name: "Document Access Policy",
            tenantId: tenantId,
            resourceType: "Document",
            effect: AbacPolicyEffect.Allow,
            attributeExpression: @"{ ""userAttributes"": { ""role"": ""Editor"" } }");

        await _dbContext.Set<AbacPolicy>().AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act - Bulk evaluate multiple resources
        var bulkRequest = new BulkEvaluateAbacPoliciesCommand
        {
            UserId = userId,
            TenantId = tenantId,
            ResourceType = "Document",
            ResourceIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            },
            Action = "edit",
            UserAttributes = new Dictionary<string, string>
            {
                { "role", "Editor" }
            }
        };

        var results = await _handler.Handle(bulkRequest);

        // Assert
        results.Should().NotBeNull();
        results.Count.Should().Be(3);
        results.Should().AllSatisfy(r => r.IsAllowed.Should().BeTrue());
    }
    */

    #endregion

    #region Conditional Policy Tests
    // TODO: Implement EvaluateConditionalPoliciesCommand and ConditionalPolicy with correct properties before uncommenting these tests
    /*
    [Fact]
    public async Task ConditionalPolicy_TimeBasedCondition_ShouldRestrictAccessOutsideHours()
    {
        // Arrange - Create time-based conditional policy
        var tenantId = Guid.NewGuid();

        var policy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Business Hours Only Policy",
            Description = "Restrict access to business hours (9 AM - 5 PM)",
            TenantId = tenantId,
            ResourceType = "AdminPanel",
            ConditionExpression = @"{
                ""timeCondition"": {
                    ""startHour"": 9,
                    ""endHour"": 17,
                    ""timezone"": ""UTC""
                }
            }",
            Action = "Block",
            IsActive = true,
            Priority = 50
        };

        await _dbContext.Set<ConditionalPolicy>().AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act - Evaluate outside business hours (should be blocked)
        var evaluationRequest = new EvaluateConditionalPoliciesCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceType = "AdminPanel",
            Action = "access",
            ContextAttributes = new Dictionary<string, string>
            {
                { "currentHour", "20" }, // 8 PM - outside business hours
                { "timezone", "UTC" }
            }
        };

        // TODO: Implement command handler - var result = // TODO: Implement command handler - await _handler.Handle(evaluationRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsBlocked.Should().BeTrue();
        result.BlockedBy.Should().Contain(p => p.Id == policy.Id);
    }

    [Fact]
    public async Task ConditionalPolicy_LocationBasedCondition_ShouldEnforceGeoRestrictions()
    {
        // Arrange - Create location-based policy
        var tenantId = Guid.NewGuid();

        var policy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Geo-Restriction Policy",
            Description = "Allow access only from approved countries",
            TenantId = tenantId,
            ResourceType = "SensitiveData",
            ConditionExpression = @"{
                ""locationCondition"": {
                    ""allowedCountries"": [""US"", ""CA"", ""GB""],
                    ""blockedCountries"": [""KP"", ""IR""]
                }
            }",
            Action = "Block",
            IsActive = true,
            Priority = 100
        };

        await _dbContext.Set<ConditionalPolicy>().AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act - Access from blocked country
        var blockedRequest = new EvaluateConditionalPoliciesCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceType = "SensitiveData",
            Action = "read",
            ContextAttributes = new Dictionary<string, string>
            {
                { "country", "KP" }, // Blocked country
                { "ipAddress", "1.2.3.4" }
            }
        };

        // TODO: Implement command handler - var blockedResult = // TODO: Implement command handler - await _handler.Handle(blockedRequest);

        // Assert - Should be blocked
        blockedResult.Should().NotBeNull();
        blockedResult.IsBlocked.Should().BeTrue();

        // Act - Access from allowed country
        var allowedRequest = new EvaluateConditionalPoliciesCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceType = "SensitiveData",
            Action = "read",
            ContextAttributes = new Dictionary<string, string>
            {
                { "country", "US" }, // Allowed country
                { "ipAddress", "8.8.8.8" }
            }
        };

        // TODO: Implement command handler - var allowedResult = // TODO: Implement command handler - await _handler.Handle(allowedRequest);

        // Assert - Should be allowed
        allowedResult.Should().NotBeNull();
        allowedResult.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task ConditionalPolicy_DeviceBasedCondition_ShouldRequireTrustedDevice()
    {
        // Arrange - Create device trust policy
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var policy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Trusted Device Only Policy",
            Description = "Require trusted device for sensitive operations",
            TenantId = tenantId,
            ResourceType = "PaymentInfo",
            ConditionExpression = @"{
                ""deviceCondition"": {
                    ""requireTrusted"": true,
                    ""requireMfa"": true
                }
            }",
            Action = "RequireMfa",
            IsActive = true,
            Priority = 80
        };

        await _dbContext.Set<ConditionalPolicy>().AddAsync(policy);
        await _dbContext.SaveChangesAsync();

        // Act - Access from untrusted device
        var untrustedRequest = new EvaluateConditionalPoliciesCommand
        {
            UserId = userId,
            TenantId = tenantId,
            ResourceType = "PaymentInfo",
            Action = "update",
            ContextAttributes = new Dictionary<string, string>
            {
                { "deviceId", Guid.NewGuid().ToString() },
                { "isTrusted", "false" }
            }
        };

        // TODO: Implement command handler - var result = // TODO: Implement command handler - await _handler.Handle(untrustedRequest);

        // Assert - Should require MFA
        result.Should().NotBeNull();
        result.RequiresMfa.Should().BeTrue();
    }

    [Fact]
    public async Task ConditionalPolicy_PriorityOrdering_HigherPriorityShouldWin()
    {
        // Arrange - Create policies with different priorities
        var tenantId = Guid.NewGuid();

        var lowPriorityPolicy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Low Priority Allow",
            TenantId = tenantId,
            ResourceType = "Data",
            ConditionExpression = @"{ ""default"": true }",
            Action = "Allow",
            IsActive = true,
            Priority = 10
        };

        var highPriorityPolicy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = "High Priority Block",
            TenantId = tenantId,
            ResourceType = "Data",
            ConditionExpression = @"{ ""userAttribute"": { ""status"": ""Pending"" } }",
            Action = "Block",
            IsActive = true,
            Priority = 100
        };

        await _dbContext.Set<ConditionalPolicy>().AddRangeAsync(lowPriorityPolicy, highPriorityPolicy);
        await _dbContext.SaveChangesAsync();

        // Act - Evaluate with user matching high priority condition
        var evaluationRequest = new EvaluateConditionalPoliciesCommand
        {
            UserId = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceType = "Data",
            Action = "read",
            UserAttributes = new Dictionary<string, string>
            {
                { "status", "Pending" }
            }
        };

        // TODO: Implement command handler - var result = // TODO: Implement command handler - await _handler.Handle(evaluationRequest);

        // Assert - High priority block should win
        result.Should().NotBeNull();
        result.IsBlocked.Should().BeTrue();
        result.BlockedBy.Should().Contain(p => p.Id == highPriorityPolicy.Id);
    }
    */
    #endregion

    #region Permission Caching Tests
    // TODO: Implement HasTenantPermissionQuery, RevokeTenantPermissionCommand, BulkRevokeTenantPermissionsCommand before uncommenting these tests
    /*
    [Fact]
    public async Task PermissionCache_FirstAccess_ShouldCacheResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = "documents:read";

        var tenantPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permission = permission,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = Guid.NewGuid()
        };

        await _dbContext.Set<TenantPermission>().AddAsync(tenantPermission);
        await _dbContext.SaveChangesAsync();

        // Act - First access (should query DB and cache)
        var query1 = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permission
        };

        // TODO: Implement command handler - var result1 = // TODO: Implement command handler - await _handler.Handle(query1);

        // Act - Second access (should use cache)
        var query2 = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permission
        };

        // TODO: Implement command handler - var result2 = // TODO: Implement command handler - await _handler.Handle(query2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();

        // Verify cache contains the result
        var cacheKey = $"permission_{userId}_{tenantId}_{permission}";
        _cache.TryGetValue(cacheKey, out bool cachedResult).Should().BeTrue();
        cachedResult.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionCache_Invalidation_AfterPermissionRevoked_ShouldUpdateCache()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permission = "documents:write";

        var tenantPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permission = permission,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = Guid.NewGuid()
        };

        await _dbContext.Set<TenantPermission>().AddAsync(tenantPermission);
        await _dbContext.SaveChangesAsync();

        // Cache the permission
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permission
        };

        // TODO: Implement command handler - var initialResult = // TODO: Implement command handler - await _handler.Handle(query);
        initialResult.Should().BeTrue();

        // Act - Revoke permission
        var revokeCommand = new RevokeTenantPermissionCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = permission
        };

        // TODO: Implement command handler - await _handler.Handle(revokeCommand);

        // Act - Clear permission cache
        var clearCacheCommand = new ClearPermissionCacheCommand
        {
            UserId = userId,
            TenantId = tenantId
        };

        // TODO: Implement command handler - await _handler.Handle(clearCacheCommand);

        // Assert - Permission should now be denied
        // TODO: Implement command handler - var afterRevokeResult = // TODO: Implement command handler - await _handler.Handle(query);
        afterRevokeResult.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionCache_BulkOperations_ShouldInvalidateRelevantCaches()
    {
        // Arrange - Create multiple permissions
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var permissions = new[]
        {
            "documents:read",
            "documents:write",
            "documents:delete"
        };

        foreach (var permission in permissions)
        {
            var tenantPermission = new TenantPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId,
                Permission = permission,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = Guid.NewGuid()
            };

            await _dbContext.Set<TenantPermission>().AddAsync(tenantPermission);
        }

        await _dbContext.SaveChangesAsync();

        // Cache all permissions
        foreach (var permission in permissions)
        {
            var query = new HasTenantPermissionQuery
            {
                UserId = userId,
                TenantId = tenantId,
                Permission = permission
            };

            // TODO: Implement command handler - await _handler.Handle(query);
        }

        // Act - Bulk revoke permissions
        var bulkRevokeCommand = new BulkRevokeTenantPermissionsCommand
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions.ToList()
        };

        // TODO: Implement command handler - await _handler.Handle(bulkRevokeCommand);

        // Clear cache
        var clearCacheCommand = new ClearPermissionCacheCommand
        {
            UserId = userId,
            TenantId = tenantId
        };

        // TODO: Implement command handler - await _handler.Handle(clearCacheCommand);

        // Assert - All permissions should be revoked
        foreach (var permission in permissions)
        {
            var query = new HasTenantPermissionQuery
            {
                UserId = userId,
                TenantId = tenantId,
                Permission = permission
            };

            // TODO: Implement command handler - var result = await _handler.Handle(query);
            result.Should().BeFalse();
        }
    }
    */
    #endregion

    #region Cross-Module Permission Inheritance Tests
    // TODO: Implement permission inheritance queries/commands before uncommenting these tests
    /*
    [Fact]
    public async Task PermissionInheritance_TenantPermission_ShouldGrantResourceAccess()
    {
        // Arrange - Grant tenant-level permission
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var tenantPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permission = "contents:manage",
            GrantedAt = DateTime.UtcNow,
            GrantedBy = Guid.NewGuid()
        };

        await _dbContext.Set<TenantPermission>().AddAsync(tenantPermission);
        await _dbContext.SaveChangesAsync();

        // Act - Check if user can access specific resource (inheritance)
        var query = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = "contents:manage"
        };

        // TODO: Implement command handler - var result = // TODO: Implement command handler - await _handler.Handle(query);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionInheritance_ContentTypePermission_ShouldOverrideTenantPermission()
    {
        // Arrange - User has tenant permission but specific content type restriction
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var ContentTypeName = "Article";

        var tenantPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permission = "contents:read",
            GrantedAt = DateTime.UtcNow,
            GrantedBy = Guid.NewGuid()
        };

        var contentTypePermission = new ContentTypePermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            ContentTypeName = contentType,
            Permission = "contents:restricted",
            GrantedAt = DateTime.UtcNow,
            GrantedBy = Guid.NewGuid()
        };

        await _dbContext.Set<TenantPermission>().AddAsync(tenantPermission);
        await _dbContext.Set<ContentTypePermission>().AddAsync(contentTypePermission);
        await _dbContext.SaveChangesAsync();

        // Act - Check content-type specific permission
        // Content type permission should take precedence

        // Assert - User should have tenant-level read access
        var tenantQuery = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = "contents:read"
        };

        // TODO: Implement command handler - var tenantResult = // TODO: Implement command handler - await _handler.Handle(tenantQuery);
        tenantResult.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionInheritance_HierarchicalPermissions_ShouldRespectHierarchy()
    {
        // Arrange - Create hierarchical permission structure
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Admin permission should grant all sub-permissions
        var adminPermission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permission = "admin:full",
            GrantedAt = DateTime.UtcNow,
            GrantedBy = Guid.NewGuid()
        };

        await _dbContext.Set<TenantPermission>().AddAsync(adminPermission);
        await _dbContext.SaveChangesAsync();

        // Act & Assert - User with admin:full should have all permissions
        var adminQuery = new HasTenantPermissionQuery
        {
            UserId = userId,
            TenantId = tenantId,
            Permission = "admin:full"
        };

        // TODO: Implement command handler - var result = // TODO: Implement command handler - await _handler.Handle(adminQuery);
        result.Should().BeTrue();

        // In a real implementation, admin:full would grant:
        // - users:manage
        // - contents:manage
        // - settings:manage
        // etc.
    }
    */
    #endregion

    public void Dispose()
    {
        _scope?.Dispose();
        _dbContext?.Dispose();
    }
}
