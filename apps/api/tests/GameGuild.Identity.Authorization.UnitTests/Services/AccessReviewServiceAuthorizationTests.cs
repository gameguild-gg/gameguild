using FluentAssertions;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class AccessReviewServiceAuthorizationTests
{
    [Fact]
    public async Task GetPendingItemsForReviewerAsync_SelfQuery_IsScopedToActorTenant()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var item = new AccessReviewItem { ReviewerId = userId };
        var campaignRepository = new Mock<IAccessReviewCampaignRepository>();
        var itemRepository = new Mock<IAccessReviewItemRepository>();
        itemRepository
            .Setup(repository => repository.GetPendingByReviewerAsync(
                userId,
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var service = CreateService(campaignRepository, itemRepository, CreateActor(userId, tenantId));

        var result = await service.GetPendingItemsForReviewerAsync(userId, tenantId);

        result.Should().ContainSingle().Which.Should().BeSameAs(item);
        itemRepository.Verify(repository => repository.GetPendingByReviewerAsync(
            userId,
            tenantId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPendingItemsForReviewerAsync_CannotReadAnotherReviewersItems()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var otherReviewerId = Guid.NewGuid();
        var itemRepository = new Mock<IAccessReviewItemRepository>();
        var service = CreateService(
            new Mock<IAccessReviewCampaignRepository>(),
            itemRepository,
            CreateActor(actorId, tenantId));

        var act = () => service.GetPendingItemsForReviewerAsync(otherReviewerId, tenantId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        itemRepository.Verify(repository => repository.GetPendingByReviewerAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPendingItemsForReviewerAsync_CannotSelectAnotherTenant()
    {
        var actorId = Guid.NewGuid();
        var actorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var itemRepository = new Mock<IAccessReviewItemRepository>();
        var service = CreateService(
            new Mock<IAccessReviewCampaignRepository>(),
            itemRepository,
            CreateActor(actorId, actorTenantId));

        var act = () => service.GetPendingItemsForReviewerAsync(actorId, otherTenantId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        itemRepository.Verify(repository => repository.GetPendingByReviewerAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveItemAsync_RejectsAnItemAssignedToAnotherReviewer()
    {
        var actorId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var item = CreateItem(reviewerId, tenantId);
        var itemRepository = new Mock<IAccessReviewItemRepository>();
        itemRepository.Setup(repository => repository.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var service = CreateService(
            new Mock<IAccessReviewCampaignRepository>(),
            itemRepository,
            CreateActor(actorId, tenantId));

        var act = () => service.ApproveItemAsync(item.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        item.Status.Should().Be(AccessReviewItemStatus.Pending);
        itemRepository.Verify(repository => repository.UpdateAsync(
            It.IsAny<AccessReviewItem>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveItemAsync_AllowsAssignedReviewerWithinTheirTenant()
    {
        var reviewerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var item = CreateItem(reviewerId, tenantId);
        var itemRepository = new Mock<IAccessReviewItemRepository>();
        itemRepository.Setup(repository => repository.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        itemRepository.Setup(repository => repository.UpdateAsync(item, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService(
            new Mock<IAccessReviewCampaignRepository>(),
            itemRepository,
            CreateActor(reviewerId, tenantId));

        var result = await service.ApproveItemAsync(item.Id);

        result.Status.Should().Be(AccessReviewItemStatus.Approved);
        itemRepository.Verify(repository => repository.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCampaignAsync_RejectsTenantAdminCampaignCreationWithSpoofedCreator()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var campaignRepository = new Mock<IAccessReviewCampaignRepository>();
        var service = CreateService(
            campaignRepository,
            new Mock<IAccessReviewItemRepository>(),
            CreateActor(actorId, tenantId, tenantAdmin: true));
        var campaign = new AccessReviewCampaign
        {
            TenantId = new TenantId(tenantId),
            CreatedBy = Guid.NewGuid()
        };

        var act = () => service.CreateCampaignAsync(campaign);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        campaignRepository.Verify(repository => repository.CreateAsync(
            It.IsAny<AccessReviewCampaign>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCampaignAsync_OnlyChangesConfigurationAndPreservesServerOwnedFields()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var existing = new AccessReviewCampaign
        {
            TenantId = new TenantId(tenantId),
            CreatedBy = Guid.NewGuid(),
            Status = AccessReviewStatus.InProgress,
            TotalItems = 12
        };
        var repository = new Mock<IAccessReviewCampaignRepository>();
        repository.Setup(item => item.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository.Setup(item => item.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreateService(
            repository,
            new Mock<IAccessReviewItemRepository>(),
            CreateActor(actorId, tenantId, tenantAdmin: true));
        var update = new AccessReviewCampaign
        {
            Id = existing.Id,
            TenantId = new TenantId(tenantId),
            Name = "Updated review",
            CreatedBy = actorId,
            Status = AccessReviewStatus.Completed,
            TotalItems = 999
        };

        var result = await service.UpdateCampaignAsync(update);

        result.Should().BeSameAs(existing);
        existing.Name.Should().Be("Updated review");
        existing.CreatedBy.Should().NotBe(actorId);
        existing.Status.Should().Be(AccessReviewStatus.InProgress);
        existing.TotalItems.Should().Be(12);
        repository.Verify(item => item.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AccessReviewItem CreateItem(Guid reviewerId, Guid tenantId) =>
        new()
        {
            ReviewerId = reviewerId,
            Campaign = new AccessReviewCampaign { TenantId = new TenantId(tenantId) }
        };

    private static AccessReviewService CreateService(
        Mock<IAccessReviewCampaignRepository> campaignRepository,
        Mock<IAccessReviewItemRepository> itemRepository,
        IActorContextAccessor actorContextAccessor) =>
        new(
            campaignRepository.Object,
            itemRepository.Object,
            NullLogger<AccessReviewService>.Instance,
            actorContextAccessor: actorContextAccessor);

    private static IActorContextAccessor CreateActor(Guid userId, Guid tenantId, bool tenantAdmin = false)
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
        accessor.SetupGet(current => current.ActorContext).Returns(actor);
        return accessor.Object;
    }
}
