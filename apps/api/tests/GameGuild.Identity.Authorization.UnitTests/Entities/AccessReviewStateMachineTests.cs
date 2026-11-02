using FluentAssertions;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Entities;

/// <summary>
///     Pins the access-review campaign and item state machines (issue #1204):
///     <see cref="AccessReviewStatus.Active"/> is reachable (Start), the first
///     decision moves Active → InProgress, both open states complete/expire/cancel,
///     decisions are Pending-only and the campaign counters advance on decisions.
/// </summary>
public sealed class AccessReviewStateMachineTests
{
    private static AccessReviewCampaign NewCampaign(
        DateTime? startDate = null,
        DateTime? endDate = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Quarterly recertification",
        StartDate = startDate ?? SystemClock.UtcNow.AddDays(-1),
        EndDate = endDate ?? SystemClock.UtcNow.AddDays(10),
        TotalItems = 2
    };

    [Fact]
    public void Start_MovesDraftToActive()
    {
        var campaign = NewCampaign();

        campaign.Start();

        campaign.Status.Should().Be(AccessReviewStatus.Active);
        campaign.IsActive().Should().BeTrue("an Active campaign inside its window is open");
        campaign.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void Start_RejectsNonDraftCampaigns()
    {
        var campaign = NewCampaign();
        campaign.Start();

        var act = campaign.Start;

        act.Should().Throw<InvalidOperationException>("a started campaign cannot be started again");
    }

    [Fact]
    public void RecordDecision_MovesActiveToInProgress_OnFirstDecision()
    {
        var campaign = NewCampaign();
        campaign.Start();

        campaign.RecordDecision(approved: true);

        campaign.Status.Should().Be(AccessReviewStatus.InProgress, "the first response moves the campaign to InProgress");
    }

    [Fact]
    public void RecordDecision_IncrementsCounters()
    {
        var campaign = NewCampaign();
        campaign.TotalItems = 3;
        campaign.Start();

        campaign.RecordDecision(approved: true);
        campaign.RecordDecision(approved: false);
        campaign.RecordDecision(approved: false);

        campaign.ReviewedItems.Should().Be(3);
        campaign.ApprovedItems.Should().Be(1);
        campaign.RevokedItems.Should().Be(2);
        campaign.GetCompletionPercentage().Should().Be(100);
    }

    [Fact]
    public void RecordDecision_KeepsInProgressCampaignsInProgress()
    {
        var campaign = NewCampaign();
        campaign.Start();
        campaign.RecordDecision(approved: true);

        campaign.RecordDecision(approved: false);

        campaign.Status.Should().Be(AccessReviewStatus.InProgress);
    }

    [Fact]
    public void Complete_IsAllowedFromActive()
    {
        var campaign = NewCampaign();
        campaign.Start();
        var completedBy = Guid.NewGuid();

        campaign.Complete(completedBy);

        campaign.Status.Should().Be(AccessReviewStatus.Completed);
        campaign.CompletedBy.Should().Be(completedBy);
        campaign.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_IsAllowedFromInProgress()
    {
        var campaign = NewCampaign();
        campaign.Start();
        campaign.RecordDecision(approved: true);

        campaign.Complete(Guid.NewGuid());

        campaign.Status.Should().Be(AccessReviewStatus.Completed);
    }

    [Theory]
    [InlineData(AccessReviewStatus.Draft)]
    [InlineData(AccessReviewStatus.Completed)]
    [InlineData(AccessReviewStatus.Expired)]
    public void Complete_RejectsNonOpenStates(AccessReviewStatus status)
    {
        var campaign = NewCampaign { Status = status };

        var act = () => campaign.Complete(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromActive_MovesToExpired()
    {
        var campaign = NewCampaign();
        campaign.Start();

        campaign.Cancel();

        campaign.Status.Should().Be(AccessReviewStatus.Expired);
    }

    [Fact]
    public void IsExpired_RecognizesActiveCampaignsPastTheirEndDate()
    {
        var campaign = NewCampaign(endDate: SystemClock.UtcNow.AddDays(-1));
        campaign.Start();

        campaign.IsExpired().Should().BeTrue("an Active campaign past its end date is expired");
    }

    [Fact]
    public void IsActive_RecognizesActiveCampaignsWithinTheirWindow()
    {
        var campaign = NewCampaign();
        campaign.Start();

        campaign.IsActive().Should().BeTrue();
    }

    [Fact]
    public void MarkExpired_FromActive_TransitionsToExpired()
    {
        var campaign = NewCampaign();
        campaign.Start();

        campaign.MarkExpired();

        campaign.Status.Should().Be(AccessReviewStatus.Expired);
    }

    [Fact]
    public void ItemApprove_FromPending_Succeeds()
    {
        var item = new AccessReviewItem { CampaignId = Guid.NewGuid() };

        item.Approve("still needed");

        item.Status.Should().Be(AccessReviewItemStatus.Approved);
        item.Decision.Should().Be(AccessReviewDecision.Approve);
        item.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void ItemRevoke_FromPending_Succeeds()
    {
        var item = new AccessReviewItem { CampaignId = Guid.NewGuid() };

        item.Revoke("no longer needed");

        item.Status.Should().Be(AccessReviewItemStatus.Revoked);
        item.Decision.Should().Be(AccessReviewDecision.Revoke);
    }

    [Fact]
    public void ItemDecisions_CannotOverwriteADecision()
    {
        var item = new AccessReviewItem { CampaignId = Guid.NewGuid() };
        item.Approve("first decision");

        var approveAgain = () => item.Approve("second decision");
        var revoke = () => item.Revoke("overwriting revoke");

        approveAgain.Should().Throw<InvalidOperationException>("decisions are final (issue #1204)");
        revoke.Should().Throw<InvalidOperationException>("decisions are final (issue #1204)");
        item.Status.Should().Be(AccessReviewItemStatus.Approved);
        item.DecisionReason.Should().Be("first decision");
    }

    [Fact]
    public void ItemRevoke_CannotOverwriteARevocation()
    {
        var item = new AccessReviewItem { CampaignId = Guid.NewGuid() };
        item.Revoke("first decision");

        var revokeAgain = () => item.Revoke("second decision");

        revokeAgain.Should().Throw<InvalidOperationException>();
        item.DecisionReason.Should().Be("first decision");
    }
}
