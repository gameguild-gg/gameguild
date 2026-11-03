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
///     Governance-enforcement integration coverage for the access-review workflow
///     (issue #1204), driven end-to-end through <see cref="ISender"/> with the real
///     host service collection (command pipeline, validators, actor context, module
///     services) against an in-memory store:
///     <list type="bullet">
///         <item><see cref="AccessReviewStatus.Active"/> is reachable (start).</item>
///         <item>The revoke decision removes the subject's in-scope permissions through
///         the grant lifecycle (enforcement, not a status flip).</item>
///         <item>Campaign counters increment on decisions.</item>
///         <item><c>AutoRevokeOnNoResponse</c> auto-revokes unanswered items at expiry.</item>
///     </list>
/// </summary>
public sealed class AccessReviewGovernanceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly string DatabaseName = $"AccessReviewGovernance_{Guid.NewGuid():N}";

    public AccessReviewGovernanceIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task Start_ReachesActive_AndFirstDecisionMovesCampaignToInProgressWithCounters()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var campaignId = await CreateCampaignAsync(adminId, tenantId);

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginAdminScope(scope, adminId, tenantId);

        (await LoadCampaignAsync(scope, campaignId))!.Status.Should().Be(AccessReviewStatus.Draft);

        var started = await sender.Send(new StartAccessReviewCampaignCommand(campaignId));
        started.Should().BeTrue();
        var campaign = (await LoadCampaignAsync(scope, campaignId))!;
        campaign.Status.Should().Be(AccessReviewStatus.Active,
            "AccessReviewStatus.Active is reachable: starting a campaign opens it (issue #1204)");

        var itemId = await SeedItemAsync(campaignId, reviewerId: adminId, subjectUserId: Guid.NewGuid(), "courses:read");
        await sender.Send(new ApproveAccessReviewItemCommand(itemId, "still required"));

        campaign = (await LoadCampaignAsync(scope, campaignId))!;
        campaign.Status.Should().Be(AccessReviewStatus.InProgress, "the first decision moves Active → InProgress");
        campaign.ReviewedItems.Should().Be(1, "campaign counters increment on decisions (issue #1204)");
        campaign.ApprovedItems.Should().Be(1);
        campaign.RevokedItems.Should().Be(0);
    }

    [Fact]
    public async Task RevokeDecision_RemovesTheSubjectsInScopePermissionsThroughTheGrantLifecycle()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaignId = await CreateCampaignAsync(adminId, tenantId);
        await SeedGrantAsync(subjectId, tenantId, "courses:read", "courses:write");
        var itemId = await SeedItemAsync(campaignId, reviewerId: adminId, subjectId, "courses:read");

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginAdminScope(scope, adminId, tenantId);

        await sender.Send(new RevokeAccessReviewItemCommand(itemId, "no longer needed"));

        var item = await LoadItemAsync(scope, itemId);
        item!.Status.Should().Be(AccessReviewItemStatus.Revoked);

        // The remediation is real: the grant row lost the revoked permission and kept
        // the out-of-scope one (grant lifecycle: remove, cache invalidation, audit row).
        var grant = await LoadGrantAsync(scope, subjectId, tenantId);
        grant.Should().NotBeNull("the out-of-scope permission survives");
        grant!.Permissions.Should().BeEquivalentTo(["courses:write"],
            "the revoke decision must remove the subject's in-scope permissions (issue #1204)");

        var campaign = (await LoadCampaignAsync(scope, campaignId))!;
        campaign.RevokedItems.Should().Be(1);
        campaign.ReviewedItems.Should().Be(1);
    }

    [Fact]
    public async Task RevokeDecision_WithFullScopeDetails_RemovesTheEntireGrantRow()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaignId = await CreateCampaignAsync(adminId, tenantId);
        await SeedGrantAsync(subjectId, tenantId, "courses:read");
        // Prose details parse to no permission tokens: full-scope revoke fallback.
        var itemId = await SeedItemAsync(campaignId, reviewerId: adminId, subjectId, "Full access recertification");

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginAdminScope(scope, adminId, tenantId);

        await sender.Send(new RevokeAccessReviewItemCommand(itemId, "full revoke"));

        var grant = await LoadGrantAsync(scope, subjectId, tenantId);
        grant.Should().BeNull("an empty grant row is deleted by the grant lifecycle");
    }

    [Fact]
    public async Task ExpirySweep_AutoRevokesUnansweredItems_WhenAutoRevokeOnNoResponseIsSet()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        // The create-command validator forbids past windows, so expired campaigns are
        // seeded directly (same shape the sweep would find in production).
        var autoCampaignId = await SeedCampaignAsync(adminId, tenantId, endDateInPast: true, autoRevoke: true);
        var manualCampaignId = await SeedCampaignAsync(adminId, tenantId, endDateInPast: true, autoRevoke: false);
        await SeedGrantAsync(subjectId, tenantId, "courses:read", "billing:manage");
        var autoItemId = await SeedItemAsync(autoCampaignId, reviewerId: adminId, subjectId, "courses:read");
        var manualItemId = await SeedItemAsync(manualCampaignId, reviewerId: adminId, subjectId, "billing:manage");

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var processed = await sender.Send(new ProcessExpiredCampaignsCommand());

        processed.Should().BeGreaterThanOrEqualTo(2);
        (await LoadCampaignAsync(scope, autoCampaignId))!.Status.Should().Be(AccessReviewStatus.Expired);
        (await LoadCampaignAsync(scope, manualCampaignId))!.Status.Should().Be(AccessReviewStatus.Expired);

        var autoItem = await LoadItemAsync(scope, autoItemId);
        autoItem!.Status.Should().Be(AccessReviewItemStatus.Revoked,
            "AutoRevokeOnNoResponse must auto-revoke unanswered items (issue #1204)");
        autoItem.Decision.Should().Be(AccessReviewDecision.Revoke);

        var manualItem = await LoadItemAsync(scope, manualItemId);
        manualItem!.Status.Should().Be(AccessReviewItemStatus.Pending,
            "campaigns without the flag only expire; their items are not decided");

        var autoCampaign = (await LoadCampaignAsync(scope, autoCampaignId))!;
        autoCampaign.RevokedItems.Should().Be(1, "the auto-revoke increments the campaign counters");
        autoCampaign.ReviewedItems.Should().Be(1);

        // Auto-revoke executed the same remediation: the in-scope permission is gone,
        // the out-of-scope one survives.
        var grant = await LoadGrantAsync(scope, subjectId, tenantId);
        grant!.Permissions.Should().BeEquivalentTo(["billing:manage"]);
    }

    [Fact]
    public async Task Decisions_CannotBeOverwritten()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var campaignId = await CreateCampaignAsync(adminId, tenantId);
        var itemId = await SeedItemAsync(campaignId, reviewerId: adminId, Guid.NewGuid(), "courses:read");

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginAdminScope(scope, adminId, tenantId);
        await sender.Send(new ApproveAccessReviewItemCommand(itemId, "first decision"));

        var overwrite = () => sender.Send(new RevokeAccessReviewItemCommand(itemId, "overwriting decision"));

        await overwrite.Should().ThrowAsync<InvalidOperationException>(
            "decisions are final — approve/revoked overwrite freely was the pre-#1204 hole");
        (await LoadItemAsync(scope, itemId))!.Status.Should().Be(AccessReviewItemStatus.Approved);
    }

    private async Task<Guid> CreateCampaignAsync(Guid adminId, Guid tenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        using var actor = BeginAdminScope(scope, adminId, tenantId);
        var campaign = await sender.Send(new CreateAccessReviewCampaignCommand(
            $"Recertification {tenantId:N}",
            "Governance integration campaign",
            tenantId,
            AccessReviewType.UserAccessReview,
            SystemClock.UtcNow.Date,
            SystemClock.UtcNow.Date.AddDays(10),
            adminId));
        campaign.Status.Should().Be(AccessReviewStatus.Draft);
        return campaign.Id;
    }

    private async Task<Guid> SeedCampaignAsync(
        Guid adminId,
        Guid tenantId,
        bool endDateInPast,
        bool autoRevoke)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var campaign = new AccessReviewCampaign
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            Name = $"Expired recertification {tenantId:N}",
            Description = "Seeded directly: the create validator forbids past windows",
            ReviewType = AccessReviewType.UserAccessReview,
            StartDate = SystemClock.UtcNow.Date.AddDays(-10),
            EndDate = endDateInPast
                ? SystemClock.UtcNow.AddDays(-1)
                : SystemClock.UtcNow.Date.AddDays(10),
            Status = AccessReviewStatus.Active,
            TotalItems = 1,
            AutoRevokeOnNoResponse = autoRevoke,
            CreatedBy = adminId
        };
        context.Set<AccessReviewCampaign>().Add(campaign);
        await context.SaveChangesAsync();
        return campaign.Id;
    }

    private static IActorContextScope BeginAdminScope(
        IServiceScope scope, Guid userId, Guid tenantId) =>
        scope.ServiceProvider
            .GetRequiredService<IActorContextAccessor>()
            .BeginActorContextScope(
                ActorContextBuilder.ForUser(userId).WithTenantId(tenantId).WithRole("TenantAdmin").Build());

    private async Task<Guid> SeedItemAsync(
        Guid campaignId, Guid reviewerId, Guid subjectUserId, string permissionDetails)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var item = new AccessReviewItem
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            ReviewerId = reviewerId,
            SubjectUserId = subjectUserId,
            PermissionDetails = permissionDetails
        };
        context.Set<AccessReviewItem>().Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    private async Task SeedGrantAsync(Guid userId, Guid tenantId, params string[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var grant = new TenantPermission
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            GrantedAt = SystemClock.UtcNow.AddDays(-30),
            GrantedBy = Guid.NewGuid(),
            Reason = "integration seed"
        };
        context.Set<TenantPermission>().Add(grant);
        await context.SaveChangesAsync();
    }

    private static async Task<AccessReviewCampaign?> LoadCampaignAsync(IServiceScope scope, Guid campaignId)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<AccessReviewCampaign>()
            .FirstOrDefaultAsync(c => c.Id == campaignId);
    }

    private static async Task<AccessReviewItem?> LoadItemAsync(IServiceScope scope, Guid itemId)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<AccessReviewItem>()
            .FirstOrDefaultAsync(i => i.Id == itemId);
    }

    private static async Task<TenantPermission?> LoadGrantAsync(IServiceScope scope, Guid userId, Guid tenantId)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Soft-deleted rows are excluded by the entity's global query filter
        // (DeletedAt == null); IsDeleted is an unmapped computed member and cannot
        // appear in a translatable predicate.
        return await context.Set<TenantPermission>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TenantId == tenantId);
    }
}
