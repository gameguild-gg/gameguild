using FluentAssertions;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class AccessReviewRepositoryScopeTests
{
    [Fact]
    public async Task GetPendingCampaignsAsync_ReturnsExpiredInProgressCampaignsOnly()
    {
        var expired = new AccessReviewCampaign
        {
            Status = AccessReviewStatus.InProgress,
            EndDate = SystemClock.UtcNow.AddMinutes(-1)
        };
        var stillActive = new AccessReviewCampaign
        {
            Status = AccessReviewStatus.InProgress,
            EndDate = SystemClock.UtcNow.AddMinutes(5)
        };
        var completed = new AccessReviewCampaign
        {
            Status = AccessReviewStatus.Completed,
            EndDate = SystemClock.UtcNow.AddMinutes(-1)
        };
        var campaigns = new[] { expired, stillActive, completed }.AsQueryable().BuildMockDbSet();
        var context = new Mock<DbContext>();
        context.Setup(db => db.Set<AccessReviewCampaign>()).Returns(campaigns.Object);

        var result = await new AccessReviewCampaignRepository(context.Object)
            .GetPendingCampaignsAsync();

        result.Should().ContainSingle().Which.Should().BeSameAs(expired);
    }

    [Fact]
    public async Task GetPendingByReviewerAsync_FiltersItemsByTheirCampaignTenant()
    {
        var reviewerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var inTenant = CreatePendingItem(reviewerId, tenantId);
        var otherTenant = CreatePendingItem(reviewerId, Guid.NewGuid());
        var campaigns = new[] { inTenant, otherTenant }.AsQueryable().BuildMockDbSet();
        var context = new Mock<DbContext>();
        context.Setup(db => db.Set<AccessReviewItem>()).Returns(campaigns.Object);

        var result = await new AccessReviewItemRepository(context.Object)
            .GetPendingByReviewerAsync(reviewerId, tenantId);

        result.Should().ContainSingle().Which.Should().BeSameAs(inTenant);
    }

    private static AccessReviewItem CreatePendingItem(Guid reviewerId, Guid tenantId)
    {
        var campaign = new AccessReviewCampaign { TenantId = new TenantId(tenantId) };
        return new AccessReviewItem
        {
            ReviewerId = reviewerId,
            CampaignId = campaign.Id,
            Campaign = campaign,
            Status = AccessReviewItemStatus.Pending
        };
    }
}
