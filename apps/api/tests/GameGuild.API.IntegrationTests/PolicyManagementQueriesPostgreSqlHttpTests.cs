using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace GameGuild.API.IntegrationTests;

/// <summary>
///     Pipeline coverage for the 15 ABAC/conditional policy read endpoints that
///     previously had no query handlers and answered 500 "Handler not found"
///     (issue #1203, docs/authorization/cqrs-wiring-matrix.md §2.1-§2.3).
/// </summary>
/// <remarks>
///     These tests exercise the real HTTP pipeline — controller, mediator, CQRS
///     behaviors and the handlers backed by the production policy stores — against
///     real PostgreSQL. The controller unit tests mock <c>IMediator</c>, which is
///     exactly how the missing handlers stayed invisible while the suite was green;
///     every request here would fail with 500 without a registered handler.
/// </remarks>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PolicyManagementQueriesPostgreSqlHttpTests(ApiPostgreSqlFixture fixture, ITestOutputHelper output)
{
    private const string AbacRoute = "/v1/abac-policies";
    private const string ConditionalRoute = "/v1/conditional-policies";

    // ---------------------------------------------------------------- ABAC

    [Fact]
    public async Task GetAbacPolicyById_ReturnsSeededPolicy_AndAnswers404ForUnknownId()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        var policyId = await SeedAbacPolicyAsync(tenantId, name: "Engineering documents", resourceType: "Document");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var found = await client.GetAsync($"{AbacRoute}/{policyId}");
        found.StatusCode.Should().Be(HttpStatusCode.OK, await found.Content.ReadAsStringAsync());
        var policy = JsonNode.Parse(await found.Content.ReadAsStringAsync())!;
        policy["id"]!.GetValue<string>().Should().Be(policyId.ToString());
        policy["name"]!.GetValue<string>().Should().Be("Engineering documents");

        var missing = await client.GetAsync($"{AbacRoute}/{Guid.NewGuid()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAbacPolicies_ListsTenantPolicies_AndAppliesTheActiveFilter()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        await SeedAbacPolicyAsync(tenantId, name: "Enabled policy", isEnabled: true);
        await SeedAbacPolicyAsync(tenantId, name: "Disabled policy", isEnabled: false);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var all = await client.GetAsync($"{AbacRoute}?tenantId={tenantId}");
        all.StatusCode.Should().Be(HttpStatusCode.OK, await all.Content.ReadAsStringAsync());
        var page = JsonNode.Parse(await all.Content.ReadAsStringAsync())!;
        page["totalCount"]!.GetValue<int>().Should().Be(2);
        page["items"]!.AsArray().Should().HaveCount(2);

        var activeOnly = await client.GetAsync($"{AbacRoute}?tenantId={tenantId}&isActive=true");
        activeOnly.StatusCode.Should().Be(HttpStatusCode.OK);
        var activePage = JsonNode.Parse(await activeOnly.Content.ReadAsStringAsync())!;
        activePage["totalCount"]!.GetValue<int>().Should().Be(1);
        var activeItems = activePage["items"]!.AsArray();
        activeItems.Should().HaveCount(1);
        activeItems[0]!["name"]!.GetValue<string>().Should().Be("Enabled policy");
    }

    [Fact]
    public async Task GetAbacPolicyStatistics_CountsTheSeededInventory()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        await SeedAbacPolicyAsync(tenantId, name: "One", isEnabled: true, tags: """["Security"]""");
        await SeedAbacPolicyAsync(tenantId, name: "Two", isEnabled: false, tags: """["Security"]""");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{AbacRoute}/statistics?tenantId={tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var statistics = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        statistics["totalPolicies"]!.GetValue<int>().Should().Be(2);
        statistics["activePolicies"]!.GetValue<int>().Should().Be(1);
        statistics["inactivePolicies"]!.GetValue<int>().Should().Be(1);
        statistics["policiesByCategory"]!["Security"]!.GetValue<int>().Should().Be(2);
        // Evaluation telemetry is not recorded yet; the counters are honestly zero.
        statistics["totalEvaluations"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task GetAbacPolicyUsage_ReturnsPolicyIdentity_WithZeroedMetrics_And404ForUnknownId()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        var policyId = await SeedAbacPolicyAsync(tenantId, name: "Usage target");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{AbacRoute}/{policyId}/usage");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var usage = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        usage["policyId"]!.GetValue<string>().Should().Be(policyId.ToString());
        usage["policyName"]!.GetValue<string>().Should().Be("Usage target");
        usage["totalEvaluations"]!.GetValue<int>().Should().Be(0);

        var missing = await client.GetAsync($"{AbacRoute}/{Guid.NewGuid()}/usage");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAbacPolicyAuditTrail_MaterializesTheEntityStamps()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        var policyId = await SeedAbacPolicyAsync(tenantId, name: "Audited policy");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{AbacRoute}/{policyId}/audit-trail");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var trail = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        trail["policyId"]!.GetValue<string>().Should().Be(policyId.ToString());
        var entries = trail["entries"]!.AsArray();
        entries.Should().HaveCount(1);
        entries[0]!["action"]!.GetValue<string>().Should().Be("Created");
        trail["totalCount"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task GetAbacPolicyConflicts_ReportsASeededAllowDenyConflict()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        await SeedAbacPolicyAsync(tenantId, name: "Allow documents", resourceType: "Document", effect: AbacPolicyEffect.Allow, priority: 5);
        await SeedAbacPolicyAsync(tenantId, name: "Deny documents", resourceType: "Document", effect: AbacPolicyEffect.Deny, priority: 5);
        await SeedAbacPolicyAsync(tenantId, name: "Unrelated workspace policy", resourceType: "Workspace", effect: AbacPolicyEffect.Allow, priority: 1);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{AbacRoute}/conflicts?tenantId={tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var analysis = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        analysis["conflicts"]!.AsArray().Should().HaveCount(1);
        analysis["conflicts"]![0]!["conflictType"]!.GetValue<string>().Should().Be("EffectConflict");
        analysis["overlaps"]!.AsArray().Should().HaveCount(1);
        analysis["overlaps"]![0]!["overlapArea"]!.GetValue<string>().Should().Be("Document");
    }

    [Fact]
    public async Task GetAbacPolicyTemplates_ReturnsTheBuiltInCatalog()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{AbacRoute}/templates");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var templates = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        templates.Should().NotBeEmpty();
        templates.Should().Contain(template => template!["name"]!.GetValue<string>() == "Department match");
    }

    // --------------------------------------------------------- Conditional

    [Fact]
    public async Task GetConditionalPolicyById_ReturnsSeededPolicy_AndAnswers404ForUnknownId()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        var policyId = await SeedConditionalPolicyAsync(tenantId, name: "Business hours deny");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var found = await client.GetAsync($"{ConditionalRoute}/{policyId}");
        found.StatusCode.Should().Be(HttpStatusCode.OK, await found.Content.ReadAsStringAsync());
        var policy = JsonNode.Parse(await found.Content.ReadAsStringAsync())!;
        policy["id"]!.GetValue<string>().Should().Be(policyId.ToString());
        policy["name"]!.GetValue<string>().Should().Be("Business hours deny");

        var missing = await client.GetAsync($"{ConditionalRoute}/{Guid.NewGuid()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConditionalPolicies_ListsTenantPolicies_AndAppliesTheConditionTypeFilter()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        await SeedConditionalPolicyAsync(tenantId, name: "Time rule", conditionType: PolicyConditionType.Time);
        await SeedConditionalPolicyAsync(tenantId, name: "Location rule", conditionType: PolicyConditionType.Location);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var all = await client.GetAsync($"{ConditionalRoute}?tenantId={tenantId}");
        all.StatusCode.Should().Be(HttpStatusCode.OK, await all.Content.ReadAsStringAsync());
        JsonNode.Parse(await all.Content.ReadAsStringAsync())!["totalCount"]!.GetValue<int>().Should().Be(2);

        var timeOnly = await client.GetAsync($"{ConditionalRoute}?tenantId={tenantId}&conditionType=Time");
        timeOnly.StatusCode.Should().Be(HttpStatusCode.OK);
        var filtered = JsonNode.Parse(await timeOnly.Content.ReadAsStringAsync())!;
        filtered["totalCount"]!.GetValue<int>().Should().Be(1);
        var filteredItems = filtered["items"]!.AsArray();
        filteredItems.Should().HaveCount(1);
        filteredItems[0]!["name"]!.GetValue<string>().Should().Be("Time rule");
    }

    [Fact]
    public async Task GetConditionalPolicyStatistics_CountsTheSeededInventory()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        await SeedConditionalPolicyAsync(tenantId, name: "One", conditionType: PolicyConditionType.Time, isEnabled: true);
        await SeedConditionalPolicyAsync(tenantId, name: "Two", conditionType: PolicyConditionType.Device, isEnabled: false);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{ConditionalRoute}/statistics?tenantId={tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var statistics = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        statistics["totalPolicies"]!.GetValue<int>().Should().Be(2);
        statistics["activePolicies"]!.GetValue<int>().Should().Be(1);
        statistics["policiesByConditionType"]!["Time"]!.GetValue<int>().Should().Be(1);
        statistics["policiesByConditionType"]!["Device"]!.GetValue<int>().Should().Be(1);
        statistics["totalEvaluations"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task GetConditionalPolicyUsage_ReturnsPolicyIdentity_WithZeroedMetrics()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        var policyId = await SeedConditionalPolicyAsync(tenantId, name: "Usage target");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{ConditionalRoute}/{policyId}/usage");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var usage = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        usage["policyId"]!.GetValue<string>().Should().Be(policyId.ToString());
        usage["policyName"]!.GetValue<string>().Should().Be("Usage target");
        usage["totalEvaluations"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task GetConditionalPolicyEvaluationHistory_IsEmptyUntilTelemetryExists_And404ForUnknownPolicy()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        var policyId = await SeedConditionalPolicyAsync(tenantId, name: "History target");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{ConditionalRoute}/{policyId}/evaluation-history");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var history = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        history["history"]!.AsArray().Should().BeEmpty();
        history["totalCount"]!.GetValue<int>().Should().Be(0);

        var missing = await client.GetAsync($"{ConditionalRoute}/{Guid.NewGuid()}/evaluation-history");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConditionalPolicyConflicts_ReportsASeededAllowDenyConflict()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);
        await SeedConditionalPolicyAsync(tenantId, name: "Allow reads", action: PolicyAction.Allow, permissionType: "Project.Read", resourceType: "Project");
        await SeedConditionalPolicyAsync(tenantId, name: "Deny reads", action: PolicyAction.Deny, permissionType: "Project.Read", resourceType: "Project");
        await SeedConditionalPolicyAsync(tenantId, name: "Unrelated writes rule", action: PolicyAction.Deny, permissionType: "Project.Write", resourceType: "Project");

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{ConditionalRoute}/conflicts?tenantId={tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var analysis = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        analysis["conflicts"]!.AsArray().Should().HaveCount(1);
        analysis["conflicts"]![0]!["conflictType"]!.GetValue<string>().Should().Be("ActionConflict");
    }

    [Fact]
    public async Task GetPolicyConditionTypes_ReturnsTheEvaluatorCatalog()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{ConditionalRoute}/condition-types");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var catalog = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        catalog.Select(type => type!["type"]!.GetValue<string>())
            .Should().BeEquivalentTo(["Time", "Environment", "Location", "Device", "RiskScore", "Custom"]);
    }

    [Fact]
    public async Task GetConditionalPolicyTemplates_ReturnsTheBuiltInCatalog()
    {
        var (tenantId, userId) = (Guid.NewGuid(), Guid.NewGuid());
        await SeedTenantAsync(tenantId, userId);

        using var client = fixture.CreateAuthenticatedClient(userId, tenantId);

        var response = await client.GetAsync($"{ConditionalRoute}/templates");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var templates = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        templates.Should().NotBeEmpty();
        templates.Should().Contain(template => template!["name"]!.GetValue<string>() == "Business hours only");
    }

    // ----------------------------------------------------------- Guards

    [Fact]
    public async Task ReadingAnotherTenantsPolicy_IsDenied()
    {
        var (tenantAId, userAId) = (Guid.NewGuid(), Guid.NewGuid());
        var tenantBId = Guid.NewGuid();
        await SeedTenantAsync(tenantAId, userAId);
        await SeedTenantAsync(tenantBId, Guid.NewGuid());
        var tenantBPolicyId = await SeedAbacPolicyAsync(tenantBId, name: "Tenant B secret policy");
        var tenantBConditionalId = await SeedConditionalPolicyAsync(tenantBId, name: "Tenant B conditional policy");

        using var client = fixture.CreateAuthenticatedClient(userAId, tenantAId);

        var abacResponse = await client.GetAsync($"{AbacRoute}/{tenantBPolicyId}");
        abacResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var abacListResponse = await client.GetAsync($"{AbacRoute}?tenantId={tenantBId}");
        abacListResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var conditionalResponse = await client.GetAsync($"{ConditionalRoute}/{tenantBConditionalId}");
        conditionalResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        output.WriteLine("Cross-tenant policy reads denied for both policy families");
    }

    // ----------------------------------------------------------- Seeding

    private async Task SeedTenantAsync(Guid tenantId, Guid userId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var marker = tenantId.ToString("N");
        context.Set<Tenant>().Add(new Tenant
        {
            Id = tenantId,
            Name = $"Policy query tenant {marker}",
            Slug = $"policy-query-{marker}",
            AdminEmail = $"admin-{marker}@policy-query.test",
            IsActive = true,
        });
        var user = User.CreateOAuthUser($"policy-query-{userId:N}@policy-query.test", $"Policy query user {userId:N}");
        user.Id = userId;
        context.Set<User>().Add(user);
        context.Set<TenantMember>().Add(new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Role = "Member",
            IsActive = true,
        });
        await context.SaveChangesAsync();
    }

    private async Task<Guid> SeedAbacPolicyAsync(
        Guid tenantId,
        string name,
        string resourceType = "Document",
        AbacPolicyEffect effect = AbacPolicyEffect.Allow,
        bool isEnabled = true,
        int priority = 1,
        string? tags = null)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var policy = new AbacPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            Name = name,
            ResourceType = resourceType,
            Effect = effect,
            IsEnabled = isEnabled,
            Priority = priority,
            Tags = tags,
        };
        context.Set<AbacPolicy>().Add(policy);
        await context.SaveChangesAsync();
        return policy.Id;
    }

    private async Task<Guid> SeedConditionalPolicyAsync(
        Guid tenantId,
        string name,
        PolicyConditionType conditionType = PolicyConditionType.Time,
        PolicyAction action = PolicyAction.Deny,
        string? permissionType = null,
        string? resourceType = null,
        bool isEnabled = true)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var policy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            Name = name,
            ConditionType = conditionType,
            Action = action,
            PermissionType = permissionType,
            ResourceType = resourceType,
            IsEnabled = isEnabled,
        };
        context.Set<ConditionalPolicy>().Add(policy);
        await context.SaveChangesAsync();
        return policy.Id;
    }
}
