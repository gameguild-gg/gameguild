using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Controllers;

public class UserMetadataControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly UserMetadataController _controller;

    public UserMetadataControllerTests()
    {
        _controller = new UserMetadataController(_sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GetMetadata_ShouldReturnOkAndNotFound()
    {
        var existingUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();
        var metadata = CreateMetadataDto(existingUserId);

        _sender.Setup(sender => sender.Send(
                It.Is<GetUserMetadataQuery>(query => query.UserId == existingUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);
        _sender.Setup(sender => sender.Send(
                It.Is<GetUserMetadataQuery>(query => query.UserId == missingUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserMetadataDto?)null);

        var getExisting = await _controller.GetMetadata(existingUserId, CancellationToken.None);
        var getMissing = await _controller.GetMetadata(missingUserId, CancellationToken.None);

        getExisting.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(metadata);
        getMissing.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateAndReplaceMetadata_ShouldMapCommandsAndReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateUserMetadataRequest(
            CustomFields: JsonMap(new Dictionary<string, object?> { ["department"] = "engineering" }),
            TagsToAdd: new List<string> { "staff" },
            TagsToRemove: new List<string> { "old" },
            ExternalReferences: new Dictionary<string, string> { ["crm"] = "123" });
        var replaceRequest = new ReplaceUserMetadataRequest(
            JsonMap(new Dictionary<string, object?> { ["department"] = "engineering" }),
            new List<string> { "staff", "lead" },
            new Dictionary<string, string> { ["crm"] = "123" });

        _sender.Setup(sender => sender.Send(
                It.Is<UpdateUserMetadataCommand>(command => command.UserId == userId && ReferenceEquals(command.Request, updateRequest)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<ReplaceUserMetadataCommand>(command => command.UserId == userId && ReferenceEquals(command.Request, replaceRequest)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var update = await _controller.UpdateMetadata(userId, updateRequest, CancellationToken.None);
        var replace = await _controller.ReplaceMetadata(userId, replaceRequest, CancellationToken.None);

        update.Should().BeOfType<NoContentResult>();
        replace.Should().BeOfType<NoContentResult>();
    }

    private static UserMetadataDto CreateMetadataDto(Guid? userId = null)
        => new(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            JsonMap(new Dictionary<string, object?> { ["department"] = "engineering" }),
            new List<string> { "staff" },
            new Dictionary<string, string> { ["crm"] = "123" },
            DateTimeOffset.UtcNow,
            null,
            new byte[] { 1 });
}
