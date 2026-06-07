using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class AccessReviewItemControllerTests
{
    [Fact]
    public async Task GetAccessReviewItems_ShouldMapFiltersAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetAccessReviewItemsQuery? captured = null;
        var campaignId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAccessReviewItemsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAccessReviewItemsQuery)request)
            .ReturnsAsync((PagedResult<AccessReviewItem>)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetAccessReviewItems(campaignId, status: "Pending", reviewerId: reviewerId, page: 3, pageSize: 25);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaignId);
        captured.Status.Should().Be("Pending");
        captured.ReviewerId.Should().Be(reviewerId);
        captured.Page.Should().Be(3);
        captured.PageSize.Should().Be(25);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReviewAccessItem_ShouldMapItemIdAndReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        ReviewAccessItemCommand? captured = null;
        var itemId = Guid.NewGuid();
        var command = new ReviewAccessItemCommand();

        mediator
            .Setup(x => x.Send(It.IsAny<ReviewAccessItemCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (ReviewAccessItemCommand)request)
            .ReturnsAsync(CreateItem());

        var controller = CreateController(mediator);

        var result = await controller.ReviewAccessItem(itemId, command);

        captured.Should().NotBeNull();
        captured!.ItemId.Should().Be(itemId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("Access review item reviewed successfully");
    }

    [Fact]
    public async Task BulkReviewAccessItems_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var command = new BulkReviewAccessItemsCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkAccessReviewResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.BulkReviewAccessItems(command);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessReviewItemDetails_ShouldMapItemIdAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetAccessReviewItemDetailsQuery? captured = null;
        var itemId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAccessReviewItemDetailsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAccessReviewItemDetailsQuery)request)
            .ReturnsAsync((AccessReviewItemDetails)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetAccessReviewItemDetails(itemId);

        captured.Should().NotBeNull();
        captured!.ItemId.Should().Be(itemId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task CreatePeriodicAccessReview_ShouldReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var command = new CreatePeriodicAccessReviewCommand();
        var periodicReview = new TestPeriodicAccessReview { Id = Guid.NewGuid(), Name = "Quarterly", Schedule = "0 0 1 * *" };

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodicReview);

        var controller = CreateController(mediator);

        var result = await controller.CreatePeriodicAccessReview(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(AccessReviewItemController.GetPeriodicAccessReview));
        created.RouteValues!["id"].Should().Be(periodicReview.Id);
        created.Value.Should().BeSameAs(periodicReview);
    }

    [Fact]
    public async Task GetPeriodicAccessReview_ShouldMapScheduleIdToReviewId()
    {
        var mediator = new Mock<IMediator>();
        GetPeriodicAccessReviewQuery? captured = null;
        var scheduleId = Guid.NewGuid();
        var periodicReview = new TestPeriodicAccessReview { Id = scheduleId };

        mediator
            .Setup(x => x.Send(It.IsAny<GetPeriodicAccessReviewQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetPeriodicAccessReviewQuery)request)
            .ReturnsAsync(periodicReview);

        var controller = CreateController(mediator);

        var result = await controller.GetPeriodicAccessReview(scheduleId);

        captured.Should().NotBeNull();
        captured!.ReviewId.Should().Be(scheduleId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(periodicReview);
    }

    [Fact]
    public async Task GetPeriodicAccessReviews_ShouldMapFiltersAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetPeriodicAccessReviewsQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetPeriodicAccessReviewsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetPeriodicAccessReviewsQuery)request)
            .ReturnsAsync((PagedResult<PeriodicAccessReview>)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetPeriodicAccessReviews(tenantId, isActive: true, page: 4, pageSize: 10);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.IsActive.Should().BeTrue();
        captured.Page.Should().Be(4);
        captured.PageSize.Should().Be(10);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TriggerPeriodicAccessReview_ShouldMapReviewIdAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        TriggerPeriodicAccessReviewCommand? captured = null;
        var scheduleId = Guid.NewGuid();
        var campaign = new AccessReviewCampaign { Id = Guid.NewGuid(), Name = "Triggered", CreatedBy = Guid.NewGuid() };

        mediator
            .Setup(x => x.Send(It.IsAny<TriggerPeriodicAccessReviewCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (TriggerPeriodicAccessReviewCommand)request)
            .ReturnsAsync(campaign);

        var controller = CreateController(mediator);

        var result = await controller.TriggerPeriodicAccessReview(scheduleId);

        captured.Should().NotBeNull();
        captured!.ReviewId.Should().Be(scheduleId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(campaign);
    }

    private static AccessReviewItemController CreateController(Mock<IMediator> mediator)
    {
        return new AccessReviewItemController(mediator.Object, NullLogger<AccessReviewItemController>.Instance);
    }

    private static AccessReviewItem CreateItem()
    {
        return new AccessReviewItem
        {
            Id = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            ReviewerId = Guid.NewGuid(),
            SubjectUserId = Guid.NewGuid(),
            PermissionDetails = "Read"
        };
    }

    private static T GetAnonymousProperty<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
    }

    private sealed class TestPeriodicAccessReview : PeriodicAccessReview;
}