using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabSettingsControllerCoverageTests
{
    [Fact]
    public async Task SettingsEndpoints_UseTheSelectedTenantAndReturnTheirResults()
    {
        var tenantId = Guid.NewGuid();
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(
            ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenantId).Build());
        var settings = new Mock<ITestingLabSettingsService>();
        var dto = new TestingLabSettingsDto { Id = Guid.NewGuid(), TenantId = tenantId, LabName = "Lab" };
        settings.Setup(value => value.GetTestingLabSettingsDtoAsync(tenantId)).ReturnsAsync(dto);
        settings.Setup(value => value.TestingLabSettingsExistAsync(tenantId)).ReturnsAsync(true);
        var sender = new Mock<ISender>();
        sender.Setup(value => value.Send(
                It.IsAny<CreateOrUpdateTestingLabSettingsEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(value => value.Send(
                It.IsAny<UpdateTestingLabSettingsEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(value => value.Send(
                It.IsAny<ResetTestingLabSettingsEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = Controller(settings.Object, sender.Object, actor.Object);

        (await controller.GetSettings()).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreateOrUpdateSettings(new CreateTestingLabSettingsDto())).Result
            .Should().BeOfType<OkObjectResult>();
        (await controller.UpdateSettings(new UpdateTestingLabSettingsDto())).Result
            .Should().BeOfType<OkObjectResult>();
        (await controller.ResetSettings()).Result.Should().BeOfType<OkObjectResult>();
        (await controller.SettingsExist()).Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(true);

        sender.Verify(value => value.Send(
            It.Is<CreateOrUpdateTestingLabSettingsEndpointCommand>(command => command.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(value => value.Send(
            It.Is<UpdateTestingLabSettingsEndpointCommand>(command => command.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(value => value.Send(
            It.Is<ResetTestingLabSettingsEndpointCommand>(command => command.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSettings_RejectsAnActorWithoutAUserId()
    {
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(ActorContext.Anonymous);
        var controller = Controller(
            Mock.Of<ITestingLabSettingsService>(), Mock.Of<ISender>(), actor.Object);

        var result = await controller.GetSettings();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Theory]
    [InlineData("replace")]
    [InlineData("update")]
    [InlineData("reset")]
    public async Task MutationEndpoints_MapInvalidArgumentsToBadRequest(string operation)
    {
        var actor = new Mock<IActorContextAccessor>();
        actor.SetupGet(value => value.ActorContext).Returns(
            ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(Guid.NewGuid()).Build());
        var sender = new Mock<ISender>();
        sender.Setup(value => value.Send(
                It.IsAny<CreateOrUpdateTestingLabSettingsEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));
        sender.Setup(value => value.Send(
                It.IsAny<UpdateTestingLabSettingsEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));
        sender.Setup(value => value.Send(
                It.IsAny<ResetTestingLabSettingsEndpointCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("invalid"));
        var controller = Controller(Mock.Of<ITestingLabSettingsService>(), sender.Object, actor.Object);

        IActionResult? result = operation switch
        {
            "replace" => (await controller.CreateOrUpdateSettings(new CreateTestingLabSettingsDto())).Result,
            "update" => (await controller.UpdateSettings(new UpdateTestingLabSettingsDto())).Result,
            _ => (await controller.ResetSettings()).Result
        };

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static TestingLabSettingsController Controller(
        ITestingLabSettingsService settings,
        ISender sender,
        IActorContextAccessor actor)
        => new(settings, sender, actor, NullLogger<TestingLabSettingsController>.Instance);
}
