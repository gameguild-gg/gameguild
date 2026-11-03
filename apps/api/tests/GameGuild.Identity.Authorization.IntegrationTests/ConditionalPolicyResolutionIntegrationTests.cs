using FluentAssertions;
using GameGuild.API;
using GameGuild.API.Database;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Commands;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GameGuild.Identity.Authorization.IntegrationTests;

/// <summary>
///     End-to-end pin for conditional policies at resolution time (issue #1204):
///     a tenant's deployed conditional deny policy must subtract permissions from the
///     subject's effective set. Driven through <see cref="ISender"/>
///     (<see cref="SimulatePermissionChangeCommand"/> resolves the current effective
///     set through the real resolver, overlays included) against the real host service
///     collection. Before #1204 the evaluator was never registered as a
///     <c>IPermissionEvaluationExtension</c>, so deployed denies never affected
///     effective-permission decisions.
/// </summary>
public sealed class ConditionalPolicyResolutionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly string DatabaseName = $"ConditionalPolicyResolution_{Guid.NewGuid():N}";

    public ConditionalPolicyResolutionIntegrationTests(WebApplicationFactory<Program> factory)
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
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            });
        });

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task DeployedConditionalDenyPolicy_SubtractsThePermissionFromEffectiveResolution()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedGrantAsync(userId, tenantId, "courses:read", "courses:write");
        await SeedDenyPolicyAsync(tenantId, "Deny course reads", permissionType: "courses:read");

        var before = await ResolveCurrentPermissionsAsync(userId, tenantId);

        before.Should().Contain("courses:write", "the un-targeted permission is unaffected");
        before.Should().NotContain("courses:read",
            "conditional policies must be consulted when permissions resolve (issue #1204)");
    }

    [Fact]
    public async Task TenantWithoutPolicies_KeepsTheAllowByNoPolicyDefault()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedGrantAsync(userId, tenantId, "courses:read", "courses:write");

        var before = await ResolveCurrentPermissionsAsync(userId, tenantId);

        before.Should().Contain(["courses:read", "courses:write"],
            "no policy configured keeps the documented allow-by-no-policy default");
    }

    [Fact]
    public async Task UnconstrainedDenyPolicy_SubtractsEverythingCurrentlyAllowed()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedGrantAsync(userId, tenantId, "courses:read", "courses:write");
        await SeedDenyPolicyAsync(tenantId, "Freeze everything", permissionType: null);

        var before = await ResolveCurrentPermissionsAsync(userId, tenantId);

        before.Should().NotContain(["courses:read", "courses:write"],
            "an unconstrained deny subtracts the whole resolution");
    }

    private async Task<IReadOnlyList<string>> ResolveCurrentPermissionsAsync(Guid userId, Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = scope.ServiceProvider
            .GetRequiredService<IActorContextAccessor>()
            .BeginActorContextScope(
                ActorContextBuilder.ForUser(userId).WithTenantId(tenantId).Build());

        // Self-what-if with a throwaway hypothetical grant (the command requires at
        // least one); Before is the resolver's output over the subject's CURRENT state.
        var simulation = await sender.Send(new SimulatePermissionChangeCommand
        {
            SubjectKind = PermissionSimulationSubjectKind.User,
            UserId = userId,
            TenantId = tenantId,
            Grants = ["hypothetical:probe"]
        });

        simulation.ContextValid.Should().BeTrue();
        return simulation.Before;
    }

    private async Task SeedGrantAsync(Guid userId, Guid tenantId, params string[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Set<TenantPermission>().Add(new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            GrantedAt = SystemClock.UtcNow.AddDays(-30),
            GrantedBy = Guid.NewGuid(),
            Reason = "integration seed"
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedDenyPolicyAsync(Guid tenantId, string name, string? permissionType)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Set<ConditionalPolicy>().Add(new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            Name = name,
            Action = PolicyAction.Deny,
            PermissionType = permissionType,
            IsEnabled = true,
            Priority = 100,
            CreatedBy = Guid.NewGuid()
        });
        await context.SaveChangesAsync();
    }
}
