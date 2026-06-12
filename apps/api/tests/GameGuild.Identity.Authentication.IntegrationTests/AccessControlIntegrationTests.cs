using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AuthPermissionService = GameGuild.Identity.Authentication.PermissionService;

namespace GameGuild.Tests.Authentication.Integration;

/// <summary>
/// Integration tests for access-control services wired through the API host.
/// </summary>
public class AccessControlIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAbacPolicyEvaluator _abacEvaluator;
    private readonly IConditionalPolicyEvaluator _conditionalEvaluator;
    private readonly AuthPermissionService _permissionService;

    public AccessControlIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
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

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"AccessControlTestDb_{Guid.NewGuid()}");
                });
                services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

                services.AddMemoryCache();
                services.AddHttpLogging(o => { });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _abacEvaluator = _scope.ServiceProvider.GetRequiredService<IAbacPolicyEvaluator>();
        _conditionalEvaluator = _scope.ServiceProvider.GetRequiredService<IConditionalPolicyEvaluator>();
        _permissionService = new AuthPermissionService(_dbContext);

        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task AbacPolicy_AllowPolicy_ShouldPermitMatchingRequest()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _dbContext.Set<AbacPolicy>().AddAsync(new AbacPolicy
        {
            Name = "Engineering document access",
            TenantId = new TenantId(tenantId),
            ResourceType = "Document",
            Effect = AbacPolicyEffect.Allow,
            Priority = 10,
            IsEnabled = true,
            SubjectConditions = """{"department":"Engineering"}""",
            ResourceConditions = """{"classification":"Internal"}""",
            ActionConditions = """{"action.id":"read"}"""
        });
        await _dbContext.SaveChangesAsync();

        var context = new AbacRequestContextBuilder()
            .WithSubject(userId, tenantId, ["Engineer"])
            .WithSubjectAttribute("department", "Engineering")
            .WithResource("Document", Guid.NewGuid())
            .WithResourceAttribute("classification", "Internal")
            .WithAction("read")
            .Build();

        var result = await _abacEvaluator.EvaluateAsync(context);

        result.Decision.Should().Be(AbacDecision.Permit);
        result.Details.Should().Contain(detail => detail.ConditionsMatched && detail.Decision == AbacDecision.Permit);
    }

    [Fact]
    public async Task AbacPolicy_DenyPolicy_ShouldOverrideMatchingAllowPolicy()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _dbContext.Set<AbacPolicy>().AddRangeAsync(
            new AbacPolicy
            {
                Name = "Allow engineering",
                TenantId = new TenantId(tenantId),
                ResourceType = "Document",
                Effect = AbacPolicyEffect.Allow,
                Priority = 10,
                IsEnabled = true,
                SubjectConditions = """{"department":"Engineering"}"""
            },
            new AbacPolicy
            {
                Name = "Deny suspended users",
                TenantId = new TenantId(tenantId),
                ResourceType = "Document",
                Effect = AbacPolicyEffect.Deny,
                Priority = 100,
                IsEnabled = true,
                SubjectConditions = """{"status":"Suspended"}"""
            });
        await _dbContext.SaveChangesAsync();

        var context = new AbacRequestContextBuilder()
            .WithSubject(userId, tenantId, ["Engineer"])
            .WithSubjectAttribute("department", "Engineering")
            .WithSubjectAttribute("status", "Suspended")
            .WithResource("Document", Guid.NewGuid())
            .WithAction("read")
            .Build();

        var result = await _abacEvaluator.EvaluateAsync(context);

        result.Decision.Should().Be(AbacDecision.Deny);
        result.DecidingPolicyName.Should().Be("Deny suspended users");
    }

    [Fact]
    public async Task ConditionalPolicy_LocationDenyPolicy_ShouldBlockMatchingCountry()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _dbContext.Set<ConditionalPolicy>().AddAsync(new ConditionalPolicy
        {
            Name = "Block restricted country",
            TenantId = new TenantId(tenantId),
            ResourceType = "AdminPanel",
            PermissionType = "access",
            Action = PolicyAction.Deny,
            Priority = 50,
            IsEnabled = true,
            LocationConditions = """{"blockedCountries":["KP","IR"]}""",
            CreatedBy = Guid.NewGuid()
        });
        await _dbContext.SaveChangesAsync();

        var result = await _conditionalEvaluator.EvaluateAsync(new ConditionalPolicyContext(
            userId,
            tenantId,
            "AdminPanel",
            null,
            "access",
            ["Admin"],
            GeoCountry: "KP"));

        result.IsAllowed.Should().BeFalse();
        result.DeniedByPolicyName.Should().Be("Block restricted country");
    }

    [Fact]
    public async Task ConditionalPolicy_NonMatchingDenyPolicy_ShouldAllowRequest()
    {
        var tenantId = Guid.NewGuid();

        await _dbContext.Set<ConditionalPolicy>().AddAsync(new ConditionalPolicy
        {
            Name = "Require low risk",
            TenantId = new TenantId(tenantId),
            ResourceType = "PaymentInfo",
            PermissionType = "update",
            Action = PolicyAction.Deny,
            Priority = 90,
            IsEnabled = true,
            EnvironmentConditions = """{"maxRiskScore":50}""",
            CreatedBy = Guid.NewGuid()
        });
        await _dbContext.SaveChangesAsync();

        var result = await _conditionalEvaluator.EvaluateAsync(new ConditionalPolicyContext(
            Guid.NewGuid(),
            tenantId,
            "PaymentInfo",
            Guid.NewGuid(),
            "update",
            ["BillingAdmin"],
            RiskScore: 90,
            IsMfaVerified: true));

        result.IsAllowed.Should().BeTrue();
        result.Details.Should().Contain(detail => detail.PolicyName == "Require low risk" && !detail.ConditionsMet);
    }

    [Fact]
    public async Task PermissionFacade_ShouldGrantResolveRevokeAndBulkCheckTenantPermissions()
    {
        var tenantId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        await _permissionService.SetTenantDefaultPermissionsAsync(tenantId, [PermissionType.Read]);
        await _permissionService.BulkGrantTenantPermissionAsync([firstUserId, secondUserId], tenantId, [PermissionType.Edit, PermissionType.Delete]);

        (await _permissionService.HasTenantPermissionAsync(firstUserId, tenantId, PermissionType.Read)).Should().BeTrue();
        (await _permissionService.HasTenantPermissionAsync(firstUserId, tenantId, PermissionType.Edit)).Should().BeTrue();
        (await _permissionService.GetUsersWithPermissionAsync(tenantId, PermissionType.Delete)).Should().BeEquivalentTo([firstUserId, secondUserId]);

        await _permissionService.RevokeTenantPermissionAsync(firstUserId, tenantId, [PermissionType.Edit, PermissionType.Delete]);

        (await _permissionService.HasTenantPermissionAsync(firstUserId, tenantId, PermissionType.Edit)).Should().BeFalse();
        (await _permissionService.HasTenantPermissionAsync(firstUserId, tenantId, PermissionType.Read)).Should().BeTrue();

        var bulk = await _permissionService.BulkCheckPermissionsAsync([firstUserId, secondUserId], tenantId, [PermissionType.Read, PermissionType.Delete]);

        bulk[firstUserId][PermissionType.Read].Should().BeTrue();
        bulk[firstUserId][PermissionType.Delete].Should().BeFalse();
        bulk[secondUserId][PermissionType.Delete].Should().BeTrue();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
