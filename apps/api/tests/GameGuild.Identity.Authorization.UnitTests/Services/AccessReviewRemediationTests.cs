using FluentAssertions;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

/// <summary>
///     Pins the access-review remediation semantics (issue #1204): the revoke
///     decision executes the grant-lifecycle revocation for the subject's in-scope
///     permissions, campaign counters advance on decisions, and the expiry sweep
///     honors <c>AutoRevokeOnNoResponse</c>.
/// </summary>
public sealed class AccessReviewRemediationTests
{
    private readonly Mock<IAccessReviewCampaignRepository> _campaignRepository = new();
    private readonly Mock<IAccessReviewItemRepository> _itemRepository = new();
    private readonly Mock<IPermissionGrantService> _grantService = new();
    private readonly Mock<ITenantPermissionRepository> _tenantPermissionRepository = new();

    private static AccessReviewCampaign CreateCampaign(Guid tenantId, bool autoRevoke = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = new TenantId(tenantId),
        Name = "Recertification",
        StartDate = SystemClock.UtcNow.AddDays(-5),
        EndDate = SystemClock.UtcNow.AddDays(5),
        Status = AccessReviewStatus.Active,
        TotalItems = 2,
        AutoRevokeOnNoResponse = autoRevoke
    };

    private static AccessReviewItem CreateItem(
        AccessReviewCampaign campaign,
        Guid reviewerId,
        Guid subjectUserId,
        string permissionDetails = "courses:read, courses:write (tenant annotation), analytics:view") =>
        new()
        {
            Id = Guid.NewGuid(),
            CampaignId = campaign.Id,
            Campaign = campaign,
            ReviewerId = reviewerId,
            SubjectUserId = subjectUserId,
            PermissionDetails = permissionDetails
        };

    private AccessReviewService CreateService(Guid actorId, Guid tenantId, bool tenantAdmin = true) =>
        new(
            _campaignRepository.Object,
            _itemRepository.Object,
            NullLogger<AccessReviewService>.Instance,
            actorContextAccessor: CreateActor(actorId, tenantId, tenantAdmin),
            permissionGrantService: _grantService.Object,
            tenantPermissionRepository: _tenantPermissionRepository.Object);

    private static IActorContextAccessor CreateActor(Guid userId, Guid tenantId, bool tenantAdmin)
    {
        var actor = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(
                tenantAdmin ? new[] { "TenantAdmin" } : Array.Empty<string>(),
                StringComparer.Ordinal),
            Permissions = new HashSet<string>(StringComparer.Ordinal),
            IsAuthenticated = true
        };
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(access => access.ActorContext).Returns(actor);
        return accessor.Object;
    }

    [Fact]
    public async Task RevokeItemAsync_RevokesInScopePermissionsThroughTheGrantLifecycle()
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaign = CreateCampaign(tenantId);
        var item = CreateItem(campaign, reviewerId, subjectId);
        _itemRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepository.Setup(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _campaignRepository.Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _grantService
            .Setup(r => r.RevokeTenantPermissionAsync(
                subjectId, tenantId, It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(reviewerId, tenantId);

        await service.RevokeItemAsync(item.Id, "No longer needed");

        _grantService.Verify(
            r => r.RevokeTenantPermissionAsync(
                subjectId,
                tenantId,
                It.Is<string[]>(permissions =>
                    permissions.Length == 3
                    && permissions.Contains("courses:read")
                    && permissions.Contains("courses:write")
                    && permissions.Contains("analytics:view")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "the revoke decision must remove the subject's in-scope permissions through the grant lifecycle");
    }

    [Fact]
    public async Task RevokeItemAsync_IncrementsCampaignCountersAndMovesCampaignToInProgress()
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaign = CreateCampaign(tenantId);
        var item = CreateItem(campaign, reviewerId, subjectId);
        _itemRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepository.Setup(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _campaignRepository.Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _grantService
            .Setup(r => r.RevokeTenantPermissionAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(reviewerId, tenantId);

        await service.RevokeItemAsync(item.Id, "No longer needed");

        campaign.Status.Should().Be(AccessReviewStatus.InProgress, "the first decision moves Active → InProgress");
        campaign.ReviewedItems.Should().Be(1);
        campaign.RevokedItems.Should().Be(1);
        campaign.ApprovedItems.Should().Be(0);
    }

    [Fact]
    public async Task ApproveItemAsync_IncrementsApprovedCounter_WithoutTouchingGrants()
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var campaign = CreateCampaign(tenantId);
        var item = CreateItem(campaign, reviewerId, Guid.NewGuid());
        _itemRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepository.Setup(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _campaignRepository.Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService(reviewerId, tenantId);

        await service.ApproveItemAsync(item.Id, "Still required");

        campaign.ApprovedItems.Should().Be(1);
        campaign.ReviewedItems.Should().Be(1);
        _grantService.Verify(
            r => r.RevokeTenantPermissionAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "approving an item keeps the subject's permissions");
    }

    [Fact]
    public async Task RevokeItemAsync_WithoutParseableDetails_RevokesEverythingTheSubjectHolds()
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaign = CreateCampaign(tenantId);
        var item = CreateItem(campaign, reviewerId, subjectId, permissionDetails: "full recertification");
        _itemRepository.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _itemRepository.Setup(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _campaignRepository.Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _grantService
            .Setup(r => r.RevokeTenantPermissionAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _tenantPermissionRepository
            .Setup(r => r.GetByUserAndTenantAsync(subjectId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission
            {
                UserId = subjectId,
                TenantId = tenantId,
                Permissions = new[] { "courses:read", "billing:manage" }
            });
        var service = CreateService(reviewerId, tenantId);

        await service.RevokeItemAsync(item.Id, "Full-scope revoke");

        _grantService.Verify(
            r => r.RevokeTenantPermissionAsync(
                subjectId,
                tenantId,
                It.Is<string[]>(permissions =>
                    permissions.Length == 2 && permissions.Contains("billing:manage")),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "an item without a parseable permission list revokes the subject's full tenant grant");
    }

    [Fact]
    public async Task ProcessExpiredCampaignsAsync_AutoRevokesUnansweredItems_WhenFlagSet()
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaign = CreateCampaign(tenantId, autoRevoke: true);
        campaign.EndDate = SystemClock.UtcNow.AddDays(-1); // expired
        var pending = CreateItem(campaign, reviewerId, subjectId, "courses:read");
        var decided = CreateItem(campaign, reviewerId, Guid.NewGuid(), "courses:write");
        decided.Approve("handled before expiry");
        _campaignRepository.Setup(r => r.GetPendingCampaignsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([campaign]);
        _campaignRepository.Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _itemRepository.Setup(r => r.GetByCampaignAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending, decided]);
        _itemRepository.Setup(r => r.UpdateAsync(It.IsAny<AccessReviewItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _grantService
            .Setup(r => r.RevokeTenantPermissionAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(reviewerId, tenantId);

        var expiredCount = await service.ProcessExpiredCampaignsAsync();

        expiredCount.Should().Be(1);
        campaign.Status.Should().Be(AccessReviewStatus.Expired);
        pending.Status.Should().Be(AccessReviewItemStatus.Revoked, "AutoRevokeOnNoResponse must auto-revoke unanswered items");
        pending.Decision.Should().Be(AccessReviewDecision.Revoke);
        decided.Status.Should().Be(AccessReviewItemStatus.Approved, "already-decided items are untouched");
        campaign.RevokedItems.Should().Be(1, "the auto-revoke increments the revoked counter");
        campaign.ReviewedItems.Should().Be(1);
        _grantService.Verify(
            r => r.RevokeTenantPermissionAsync(
                subjectId, tenantId, It.Is<string[]>(p => p.Length == 1 && p.Contains("courses:read")), It.IsAny<CancellationToken>()),
            Times.Once,
            "the auto-revoke executes the same permission remediation as the manual decision");
    }

    [Fact]
    public async Task ProcessExpiredCampaignsAsync_LeavesUnansweredItemsPending_WhenFlagUnset()
    {
        var tenantId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var campaign = CreateCampaign(tenantId, autoRevoke: false);
        campaign.EndDate = SystemClock.UtcNow.AddDays(-1);
        var pending = CreateItem(campaign, reviewerId, subjectId, "courses:read");
        _campaignRepository.Setup(r => r.GetPendingCampaignsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([campaign]);
        _campaignRepository.Setup(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _itemRepository.Setup(r => r.GetByCampaignAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([pending]);
        _itemRepository.Setup(r => r.UpdateAsync(It.IsAny<AccessReviewItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService(reviewerId, tenantId);

        await service.ProcessExpiredCampaignsAsync();

        campaign.Status.Should().Be(AccessReviewStatus.Expired);
        pending.Status.Should().Be(AccessReviewItemStatus.Pending, "without the flag the sweep only expires the campaign");
        campaign.RevokedItems.Should().Be(0);
        _grantService.Verify(
            r => r.RevokeTenantPermissionAsync(
                It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("courses:read, courses:write", 2)]
    [InlineData("courses:read; billing:manage", 2)]
    [InlineData("courses:read (tenant abc)", 1)]
    [InlineData("  ", 0)]
    [InlineData(null, 0)]
    [InlineData("full recertification", 0)]
    public void ParsePermissionDetails_ExtractsDistinctPermissionTokens(string? details, int expectedCount)
    {
        var parsed = AccessReviewService.ParsePermissionDetails(details);

        parsed.Should().HaveCount(expectedCount);
        parsed.Should().OnlyContain(p => !p.Contains('(') && !p.Contains(' ') && p == p.Trim());
    }

    [Fact]
    public void ParsePermissionDetails_IsCaseInsensitivelyDistinct()
    {
        var parsed = AccessReviewService.ParsePermissionDetails("courses:read, Courses:Read");

        parsed.Should().ContainSingle().Which.Should().Be("courses:read");
    }
}
