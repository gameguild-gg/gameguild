using FluentAssertions;
using GameGuild.Commerce.Products;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Products.Unit.Services;

/// <summary>
/// Issue #418 campaign management: the PromoCampaign entity enforces its lifecycle
/// transitions, and the CRUD command handlers guard the invariant operations
/// (duplicate keys, delete-with-referencing-codes).
/// </summary>
public class PromoCampaignLifecycleTests
{
    [Fact]
    public void NewCampaign_StartsInDraft()
    {
        var campaign = new PromoCampaign { Name = "Black Friday", Key = "bf-2026" };

        campaign.Status.Should().Be(PromoCampaignStatus.Draft);
    }

    [Fact]
    public void Activate_FromDraft_MovesToActive()
    {
        var campaign = new PromoCampaign { Name = "N", Key = "K" };

        campaign.Activate();

        campaign.Status.Should().Be(PromoCampaignStatus.Active);
    }

    [Fact]
    public void Activate_FromPaused_MovesToActive()
    {
        var campaign = Paused();

        campaign.Activate();

        campaign.Status.Should().Be(PromoCampaignStatus.Active);
    }

    [Fact]
    public void Pause_FromActive_MovesToPaused()
    {
        var campaign = Active();

        campaign.Pause();

        campaign.Status.Should().Be(PromoCampaignStatus.Paused);
    }

    [Fact]
    public void Pause_FromDraft_Throws()
    {
        var campaign = new PromoCampaign { Name = "N", Key = "K" };

        var act = () => campaign.Pause();

        act.Should().Throw<InvalidOperationException>().WithMessage("*pause*Draft*");
    }

    [Fact]
    public void Complete_FromActive_MovesToCompleted()
    {
        var campaign = Active();

        campaign.Complete();

        campaign.Status.Should().Be(PromoCampaignStatus.Completed);
    }

    [Fact]
    public void Cancel_FromActive_MovesToCancelled()
    {
        var campaign = Active();

        campaign.Cancel();

        campaign.Status.Should().Be(PromoCampaignStatus.Cancelled);
    }

    [Fact]
    public void Archive_FromCompleted_MovesToArchived()
    {
        var campaign = Active();
        campaign.Complete();

        campaign.Archive();

        campaign.Status.Should().Be(PromoCampaignStatus.Archived);
    }

    [Fact]
    public void Archive_FromDraft_Throws()
    {
        var campaign = new PromoCampaign { Name = "N", Key = "K" };

        var act = () => campaign.Archive();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromCompleted_Throws()
    {
        var campaign = Active();
        campaign.Complete();

        var act = () => campaign.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsWindowOpen_RespectsBothEdges()
    {
        var now = SystemClock.UtcNow;
        var open = new PromoCampaign { StartsAtUtc = now.AddDays(-1), EndsAtUtc = now.AddDays(1) };
        var notStarted = new PromoCampaign { StartsAtUtc = now.AddDays(1) };
        var closed = new PromoCampaign { EndsAtUtc = now.AddDays(-1) };

        open.IsWindowOpen(now).Should().BeTrue();
        notStarted.IsWindowOpen(now).Should().BeFalse();
        closed.IsWindowOpen(now).Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateKey_Throws()
    {
        var campaignRepository = new Mock<IPromoCampaignRepository>();
        campaignRepository.Setup(r => r.KeyExistsAsync("BF26", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreatePromoCampaignCommandHandler(
            campaignRepository.Object, Mock.Of<IPromoCodeAuditLogger>());

        var act = async () => await handler.Handle(
            new CreatePromoCampaignCommand("Black Friday", "BF26"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BF26*");
    }

    [Fact]
    public async Task Create_NormalizesWindowToUtcAndPersistsTimeZone()
    {
        var campaignRepository = new Mock<IPromoCampaignRepository>();
        campaignRepository.Setup(r => r.KeyExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        PromoCampaign? persisted = null;
        campaignRepository
            .Setup(r => r.AddAsync(It.IsAny<PromoCampaign>(), It.IsAny<CancellationToken>()))
            .Callback<PromoCampaign, CancellationToken>((campaign, _) => persisted = campaign)
            .Returns(ValueTask.CompletedTask);
        var handler = new CreatePromoCampaignCommandHandler(
            campaignRepository.Object, Mock.Of<IPromoCodeAuditLogger>());

        var saoPauloWallClock = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var dto = await handler.Handle(
            new CreatePromoCampaignCommand(
                "Black Friday",
                "BF26",
                StartsAt: saoPauloWallClock,
                TimeZoneId: "America/Sao_Paulo",
                AcquisitionSpend: 500m),
            CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.TimeZoneId.Should().Be("America/Sao_Paulo");
        persisted.StartsAtUtc.Should().NotBeNull();
        persisted.StartsAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        persisted.StartsAtUtc.Value.Should().Be(
            TimeZoneInfo.ConvertTimeToUtc(saoPauloWallClock, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));
        dto.Status.Should().Be(PromoCampaignStatus.Draft);
        dto.Key.Should().Be("BF26");
    }

    [Fact]
    public async Task Delete_WithReferencingCodes_FailsClosed()
    {
        var campaignRepository = new Mock<IPromoCampaignRepository>();
        var campaign = new PromoCampaign { Id = Guid.NewGuid(), Name = "N", Key = "BF26" };
        campaignRepository.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
        campaignRepository.Setup(r => r.CountCodesForKeyAsync("BF26", It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);
        var handler = new DeletePromoCampaignCommandHandler(
            campaignRepository.Object, Mock.Of<IPromoCodeAuditLogger>());

        var act = async () => await handler.Handle(
            new DeletePromoCampaignCommand(campaign.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*7 promo code*");
        campaignRepository.Verify(r => r.DeleteAsync(It.IsAny<PromoCampaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithoutReferencingCodes_Deletes()
    {
        var campaignRepository = new Mock<IPromoCampaignRepository>();
        var campaign = new PromoCampaign { Id = Guid.NewGuid(), Name = "N", Key = "BF26" };
        campaignRepository.Setup(r => r.GetByIdAsync(campaign.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
        campaignRepository.Setup(r => r.CountCodesForKeyAsync("BF26", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var handler = new DeletePromoCampaignCommandHandler(
            campaignRepository.Object, Mock.Of<IPromoCodeAuditLogger>());

        var deleted = await handler.Handle(new DeletePromoCampaignCommand(campaign.Id), CancellationToken.None);

        deleted.Should().BeTrue();
        campaignRepository.Verify(r => r.DeleteAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnknownCampaign_ThrowsNotFound()
    {
        var campaignRepository = new Mock<IPromoCampaignRepository>();
        campaignRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PromoCampaign?)null);
        var handler = new DeletePromoCampaignCommandHandler(
            campaignRepository.Object, Mock.Of<IPromoCodeAuditLogger>());

        var act = async () => await handler.Handle(
            new DeletePromoCampaignCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<PromoCampaignNotFoundException>();
    }

    private static PromoCampaign Active()
    {
        var campaign = new PromoCampaign { Name = "N", Key = "K" };
        campaign.Activate();
        return campaign;
    }

    private static PromoCampaign Paused()
    {
        var campaign = Active();
        campaign.Pause();
        return campaign;
    }
}
