using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public sealed class AccessReviewCampaignControllerTests
{
    [Fact]
    public async Task CreateAccessReviewCampaign_ShouldReturnCreatedAtAction()
    {
        var mediator = new Mock<IMediator>();
        var command = new CreateAccessReviewCampaignCommand();
        var campaign = CreateCampaign();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var controller = CreateController(mediator);

        var result = await controller.CreateAccessReviewCampaign(command);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(AccessReviewCampaignController.GetAccessReviewCampaign));
        created.RouteValues!["id"].Should().Be(campaign.Id);
        created.Value.Should().BeSameAs(campaign);
    }

    [Fact]
    public async Task GetAccessReviewCampaign_ShouldReturnOkAndMapCampaignId()
    {
        var mediator = new Mock<IMediator>();
        GetAccessReviewCampaignQuery? captured = null;
        var campaign = CreateCampaign();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAccessReviewCampaignQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAccessReviewCampaignQuery)request)
            .ReturnsAsync(campaign);

        var controller = CreateController(mediator);

        var result = await controller.GetAccessReviewCampaign(campaign.Id);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaign.Id);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(campaign);
    }

    [Fact]
    public async Task UpdateAccessReviewCampaign_ShouldMapCampaignIdThroughWithExpression()
    {
        var mediator = new Mock<IMediator>();
        UpdateAccessReviewCampaignCommand? captured = null;
        var command = new UpdateAccessReviewCampaignCommand();
        var campaign = CreateCampaign();

        mediator
            .Setup(x => x.Send(It.IsAny<UpdateAccessReviewCampaignCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (UpdateAccessReviewCampaignCommand)request)
            .ReturnsAsync(campaign);

        var controller = CreateController(mediator);

        var result = await controller.UpdateAccessReviewCampaign(campaign.Id, command);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaign.Id);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(campaign);
    }

    [Fact]
    public async Task DeleteAccessReviewCampaign_ShouldReturnNoContent()
    {
        var mediator = new Mock<IMediator>();
        DeleteAccessReviewCampaignCommand? captured = null;
        var campaignId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<DeleteAccessReviewCampaignCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((request, _) => captured = (DeleteAccessReviewCampaignCommand)request)
            .ReturnsAsync(true);

        var controller = CreateController(mediator);

        var result = await controller.DeleteAccessReviewCampaign(campaignId);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaignId);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetAccessReviewCampaigns_ShouldMapFiltersAndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        GetAccessReviewCampaignsQuery? captured = null;
        var tenantId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAccessReviewCampaignsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (GetAccessReviewCampaignsQuery)request)
            .ReturnsAsync((PagedResult<AccessReviewCampaign>)null!);

        var controller = CreateController(mediator);

        var result = await controller.GetAccessReviewCampaigns(tenantId, status: "Draft", type: "Manager", page: 2, pageSize: 30);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
        captured.Status.Should().Be("Draft");
        captured.Type.Should().Be("Manager");
        captured.Page.Should().Be(2);
        captured.PageSize.Should().Be(30);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartAccessReviewCampaign_ShouldReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        StartAccessReviewCampaignCommand? captured = null;
        var campaignId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<StartAccessReviewCampaignCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((request, _) => captured = (StartAccessReviewCampaignCommand)request)
            .ReturnsAsync(true);

        var controller = CreateController(mediator);

        var result = await controller.StartAccessReviewCampaign(campaignId);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaignId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("Access review campaign started successfully");
    }

    [Fact]
    public async Task CompleteAccessReviewCampaign_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        CompleteAccessReviewCampaignCommand? captured = null;
        var campaignId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<CompleteAccessReviewCampaignCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (CompleteAccessReviewCampaignCommand)request)
            .ReturnsAsync((AccessReviewCampaignResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.CompleteAccessReviewCampaign(campaignId);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaignId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task SendReviewReminders_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        SendReviewRemindersCommand? captured = null;
        var campaignId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<SendReviewRemindersCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (SendReviewRemindersCommand)request)
            .ReturnsAsync((ReminderResult)null!);

        var controller = CreateController(mediator);

        var result = await controller.SendReviewReminders(campaignId);

        captured.Should().NotBeNull();
        captured!.CampaignId.Should().Be(campaignId);
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task ConfigureReminderSettings_ShouldReturnSuccessMessage()
    {
        var mediator = new Mock<IMediator>();
        var command = new ConfigureReminderSettingsCommand();

        mediator
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(mediator);

        var result = await controller.ConfigureReminderSettings(command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        GetAnonymousProperty<string>(ok.Value!, "message").Should().Be("Reminder settings configured successfully");
    }

    [Fact]
    public async Task GetAccessReviewTemplates_ShouldReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var templates = Array.Empty<AccessReviewTemplateDto>();

        mediator
            .Setup(x => x.Send(It.IsAny<GetAccessReviewTemplatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates.AsEnumerable());

        var controller = CreateController(mediator);

        var result = await controller.GetAccessReviewTemplates();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(templates);
    }

    [Fact]
    public async Task CreateCampaignFromTemplate_ShouldMapTemplateIdThroughWithExpression()
    {
        var mediator = new Mock<IMediator>();
        CreateCampaignFromTemplateCommand? captured = null;
        var command = new CreateCampaignFromTemplateCommand();
        var campaign = CreateCampaign();
        var templateId = Guid.NewGuid();

        mediator
            .Setup(x => x.Send(It.IsAny<CreateCampaignFromTemplateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((request, _) => captured = (CreateCampaignFromTemplateCommand)request)
            .ReturnsAsync(campaign);

        var controller = CreateController(mediator);

        var result = await controller.CreateCampaignFromTemplate(templateId, command);

        captured.Should().NotBeNull();
        captured!.TemplateId.Should().Be(templateId);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.RouteValues!["id"].Should().Be(campaign.Id);
        created.Value.Should().BeSameAs(campaign);
    }

    private static AccessReviewCampaignController CreateController(Mock<IMediator> mediator)
    {
        return new AccessReviewCampaignController(mediator.Object, NullLogger<AccessReviewCampaignController>.Instance);
    }

    private static AccessReviewCampaign CreateCampaign()
    {
        return new AccessReviewCampaign
        {
            Id = Guid.NewGuid(),
            Name = "Quarterly Access Review",
            CreatedBy = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        };
    }

    private static T GetAnonymousProperty<T>(object target, string propertyName)
    {
        return (T)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
    }
}