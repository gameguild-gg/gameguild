using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Controllers;

public class UserProfilesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly UserProfilesController _controller;

    public UserProfilesControllerTests()
    {
        _controller = new UserProfilesController(_sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GetProfiles_ShouldMapQueryAndReturnOk()
    {
        var result = PagedResult<UserProfileDto>.FromPage(new[] { CreateProfileDto() }, totalCount: 1, pageNumber: 2, pageSize: 10);

        _sender.Setup(sender => sender.Send(
                It.Is<GetUserProfilesPagedQuery>(query =>
                    query.Search == "designer" &&
                    query.SortBy == "displayName" &&
                    query.SortDirection == "desc" &&
                    query.PageNumber == 2 &&
                    query.PageSize == 10),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetProfiles(
            page: 2,
            pageSize: 10,
            search: "designer",
            sortBy: "displayName",
            sortDirection: "desc",
            ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task GetProfile_ShouldReturnOkAndNotFound()
    {
        var existingUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();
        var profile = CreateProfileDto(existingUserId);

        _sender.Setup(sender => sender.Send(
                It.Is<GetUserProfileQuery>(query => query.UserId == existingUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _sender.Setup(sender => sender.Send(
                It.Is<GetUserProfileQuery>(query => query.UserId == missingUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        var getExisting = await _controller.GetProfile(existingUserId, CancellationToken.None);
        var getMissing = await _controller.GetProfile(missingUserId, CancellationToken.None);

        getExisting.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(profile);
        getMissing.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateAndReplaceProfile_ShouldMapCommandsAndReturnPersistedProfile()
    {
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateUserProfileRequest(DisplayName: "Matheus", Bio: "Bio");
        var replaceRequest = new ReplaceUserProfileRequest(
            DisplayName: "Matheus",
            Bio: "Bio",
            Location: "Sao Paulo",
            Website: "https://example.com",
            JobTitle: "Engineer",
            Company: "GameGuild",
            TimeZone: "America/Sao_Paulo",
            Language: "pt-BR",
            ProfileVisibility: "public",
            ShowEmail: true,
            ShowLocation: true);
        var profile = CreateProfileDto(userId);

        _sender.Setup(sender => sender.Send(
                It.Is<UpdateUserProfileCommand>(command => command.UserId == userId && ReferenceEquals(command.Request, updateRequest)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _sender.Setup(sender => sender.Send(
                It.Is<ReplaceUserProfileCommand>(command => command.UserId == userId && ReferenceEquals(command.Request, replaceRequest)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var update = await _controller.UpdateProfile(userId, updateRequest, CancellationToken.None);
        var replace = await _controller.ReplaceProfile(userId, replaceRequest, CancellationToken.None);

        update.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(profile);
        replace.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(profile);
    }

    private static UserProfileDto CreateProfileDto(Guid? userId = null)
        => new(
            Guid.NewGuid(),
            userId ?? Guid.NewGuid(),
            "Display",
            "Bio",
            "Sao Paulo",
            "https://example.com",
            "Engineer",
            "GameGuild",
            null,
            null,
            "America/Sao_Paulo",
            "pt-BR",
            "public",
            true,
            true,
            DateTimeOffset.UtcNow,
            null,
            new byte[] { 1 });
}
