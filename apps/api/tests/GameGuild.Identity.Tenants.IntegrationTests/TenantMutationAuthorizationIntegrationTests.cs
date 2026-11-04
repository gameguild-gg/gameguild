using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Identity.Tenants.IntegrationTests;

/// <summary>
///     Authorization posture of the tenant mutation surface (issue #1287 T1/T9),
///     driven through <see cref="ISender"/> with the real host service collection
///     (command pipeline, module services, the tenant administration guard) against
///     an in-memory store.
/// </summary>
/// <remarks>
///     The guard denials surface as <see cref="GameGuild.AccessDeniedException"/>,
///     which the shared exception middleware maps to HTTP 403 — these tests pin the
///     pipeline-level contract the controllers sit on top of. Before the fix every
///     one of these mutations succeeded for ANY authenticated principal.
/// </remarks>
public sealed class TenantMutationAuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<GameGuild.API.Program>>, IDisposable
{
    private readonly WebApplicationFactory<GameGuild.API.Program> _factory;
    private readonly IActorContextAccessor _actorAccessor;

    public TenantMutationAuthorizationIntegrationTests(WebApplicationFactory<GameGuild.API.Program> factory)
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
                    options.UseInMemoryDatabase($"TenantMutationAuthz_{Guid.NewGuid()}");
                });
                services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            });
        });

        using (var seedScope = _factory.Services.CreateScope())
        {
            seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        }

        _actorAccessor = _factory.Services.GetRequiredService<IActorContextAccessor>();
    }

    public void Dispose() => _actorAccessor.ClearActorContext();

    [Fact]
    public async Task EveryTenantMutation_IsDeniedForAnAuthenticatedNonAdministrator()
    {
        var tenantId = await SeedTenantAsync(isArchived: false);
        var strangerId = Guid.NewGuid();
        await SeedMembershipAsync(tenantId, strangerId, TenantRole.Member.Value);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginUserScope(strangerId, tenantId);

        // The takeover class from the issue: an ordinary authenticated principal (even
        // a plain member of the tenant) previously could update, deactivate, archive,
        // undelete, purge, rewrite metadata and bulk-mutate ANY tenant. Each mutation
        // must now be denied with the platform's 403 signal.
        await AssertDeniedAsync(() => sender.Send(new UpdateTenantCommand(tenantId, "Hostile Takeover", null)));
        await AssertDeniedAsync(() => sender.Send(new DeactivateTenantCommand(tenantId)));
        await AssertDeniedAsync(() => sender.Send(new ActivateTenantCommand(tenantId)));
        await AssertDeniedAsync(() => sender.Send(new ArchiveTenantCommand(tenantId, "hostile")));
        await AssertDeniedAsync(() => sender.Send(new RecoverTenantCommand(tenantId, "hostile")));
        await AssertDeniedAsync(() => sender.Send(new DeleteTenantCommand(tenantId)));
        await AssertDeniedAsync(() => sender.Send(new UpdateTenantTagsCommand(tenantId, new UpdateTenantTagsRequest(["hostile"]))));
        await AssertDeniedAsync(() => sender.Send(new ReplaceTenantTagsCommand(tenantId, new ReplaceTenantTagsRequest(["hostile"]))));
        await AssertDeniedAsync(() => sender.Send(new UpdateTenantCustomFieldsCommand(tenantId, new UpdateTenantCustomFieldsRequest(new Dictionary<string, object?> { ["hostile"] = "1" }))));
        await AssertDeniedAsync(() => sender.Send(new BulkUpdateTenantsCommand([new BulkUpdateTenantItem(tenantId, "Hostile Takeover")])));

        var tenant = await LoadTenantAsync(scope, tenantId);
        tenant!.Name.Should().NotBe("Hostile Takeover", "nothing was mutated");
        tenant.IsActive.Should().BeTrue();
        tenant.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task TenantMutations_AreDeniedForACompleteStranger()
    {
        var tenantId = await SeedTenantAsync(isArchived: false);
        var strangerId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginUserScope(strangerId, tenantId);

        await AssertDeniedAsync(() => sender.Send(new UpdateTenantCommand(tenantId, "Hostile Takeover", null)));
        await AssertDeniedAsync(() => sender.Send(new DeleteTenantCommand(tenantId)));
    }

    [Fact]
    public async Task BulkMutations_AreDeniedWhenAnyTargetIsNotAdministrable()
    {
        var ownedTenantId = await SeedTenantAsync(isArchived: false);
        var foreignTenantId = await SeedTenantAsync(isArchived: false);
        var adminId = Guid.NewGuid();
        await SeedMembershipAsync(ownedTenantId, adminId, TenantRole.Admin.Value);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginUserScope(adminId, ownedTenantId);

        // Pre-flighted: nothing is mutated even for the tenant the actor DOES manage.
        await AssertDeniedAsync(() => sender.Send(new BulkDeactivateTenantsCommand([ownedTenantId, foreignTenantId])));

        var owned = await LoadTenantAsync(scope, ownedTenantId);
        var foreign = await LoadTenantAsync(scope, foreignTenantId);
        owned!.IsActive.Should().BeTrue("the pre-flight denied the batch before any mutation");
        foreign!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Purge_IsDeniedForAnAdminMembership_AndAllowedForTheOwner()
    {
        var archivedTenantId = await SeedTenantAsync(isArchived: true);
        var adminId = Guid.NewGuid();
        await SeedMembershipAsync(archivedTenantId, adminId, TenantRole.Admin.Value);

        using (var scope = _factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            using var actor = BeginUserScope(adminId, archivedTenantId);

            // Tenancy model: Admin = "full management capabilities except tenant deletion".
            await AssertDeniedAsync(() => sender.Send(new DeleteTenantCommand(archivedTenantId)));
            await AssertDeniedAsync(() => sender.Send(new BulkPurgeTenantsCommand([archivedTenantId])));
        }

        var liveTenantId = await SeedTenantAsync(isArchived: true);
        var ownerId = Guid.NewGuid();
        await SeedMembershipAsync(liveTenantId, ownerId, TenantRole.Owner.Value);

        using (var scope = _factory.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            using var actor = BeginUserScope(ownerId, liveTenantId);

            await sender.Send(new DeleteTenantCommand(liveTenantId));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await context.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == liveTenantId)).Should().BeNull("the Owner purge deleted the tenant");
        }
    }

    [Fact]
    public async Task Mutations_AreAllowedForTheOwnerMembership()
    {
        var tenantId = await SeedTenantAsync(isArchived: false);
        var ownerId = Guid.NewGuid();
        await SeedMembershipAsync(tenantId, ownerId, TenantRole.Owner.Value);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginUserScope(ownerId, tenantId);

        await sender.Send(new UpdateTenantCommand(tenantId, "Renamed by owner", "new description"));
        var archived = await sender.Send(new ArchiveTenantCommand(tenantId, "owner decision"));
        archived.Success.Should().BeTrue();
        var recovered = await sender.Send(new RecoverTenantCommand(tenantId, "owner decision reversed"));
        recovered.Success.Should().BeTrue();
        await sender.Send(new UpdateTenantTagsCommand(tenantId, new UpdateTenantTagsRequest(["owner-tag"])));
        var bulk = await sender.Send(new BulkDeactivateTenantsCommand([tenantId]));
        bulk.SuccessfulOperations.Should().Be(1);

        var tenant = await LoadTenantAsync(scope, tenantId);
        tenant!.Name.Should().Be("Renamed by owner");
        tenant.IsArchived.Should().BeFalse();
        tenant.IsActive.Should().BeFalse("the bulk deactivate ran last");
    }

    [Fact]
    public async Task Mutations_AreAllowedForTheAdminMembership()
    {
        var tenantId = await SeedTenantAsync(isArchived: false);
        var adminId = Guid.NewGuid();
        await SeedMembershipAsync(tenantId, adminId, TenantRole.Admin.Value);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginUserScope(adminId, tenantId);

        await sender.Send(new UpdateTenantCommand(tenantId, "Renamed by admin", null));

        (await LoadTenantAsync(scope, tenantId))!.Name.Should().Be("Renamed by admin");
    }

    [Fact]
    public async Task Mutations_AreAllowedForAPlatformAdministratorWithoutMembership()
    {
        var tenantId = await SeedTenantAsync(isArchived: false);
        var systemAdminId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginUserScope(systemAdminId, tenantId, roles: ["SystemAdmin"]);

        await sender.Send(new UpdateTenantCommand(tenantId, "Renamed by platform admin", null));

        (await LoadTenantAsync(scope, tenantId))!.Name.Should().Be("Renamed by platform admin");
    }

    private static async Task AssertDeniedAsync(Func<Task> act) =>
        await act.Should().ThrowAsync<GameGuild.AccessDeniedException>();

    private IActorContextScope BeginUserScope(Guid userId, Guid tenantId, string[]? roles = null)
    {
        var builder = ActorContextBuilder.ForUser(userId).WithTenantId(tenantId);
        if (roles is not null)
        {
            builder = builder.WithRoles(roles);
        }

        return _actorAccessor.BeginActorContextScope(builder.Build());
    }

    private async Task<Guid> SeedTenantAsync(bool isArchived)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = $"Tenant {Guid.NewGuid():N}",
            Slug = $"tenant-{Guid.NewGuid():N}",
            AdminEmail = "admin@example.com",
            IsActive = !isArchived,
            IsArchived = isArchived
        };
        await context.Set<Tenant>().AddAsync(tenant);
        await context.SaveChangesAsync();
        return tenant.Id;
    }

    private async Task SeedMembershipAsync(Guid tenantId, Guid userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Set<TenantMember>().AddAsync(new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Role = role,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static async Task<Tenant?> LoadTenantAsync(IServiceScope scope, Guid tenantId)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<Tenant>().FirstOrDefaultAsync(t => t.Id == tenantId);
    }
}
