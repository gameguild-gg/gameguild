using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

public class GetUserProfilesPagedQueryHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profileRepositoryMock;
    private readonly GetUserProfilesPagedQueryHandler _handler;

    public GetUserProfilesPagedQueryHandlerTests()
    {
        _profileRepositoryMock = new Mock<IUserProfileRepository>();
        _handler = new GetUserProfilesPagedQueryHandler(_profileRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldMapProfiles_AndPreservePageMetadata()
    {
        var firstProfile = UserProfile.Create(Guid.NewGuid(), "Display One");
        firstProfile.Id = Guid.NewGuid();
        firstProfile.Bio = "Bio";
        firstProfile.Location = "Sao Paulo";
        firstProfile.Website = "https://example.com";
        firstProfile.JobTitle = "Engineer";
        firstProfile.Company = "GameGuild";
        firstProfile.AvatarUrl = "https://example.com/avatar.png";
        firstProfile.BannerUrl = "https://example.com/banner.png";
        firstProfile.Visibility = ProfileVisibility.FriendsOnly;
        firstProfile.CreatedAt = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        firstProfile.UpdatedAt = new DateTime(2024, 8, 2, 0, 0, 0, DateTimeKind.Utc);
        firstProfile.Version = 4;

        var secondProfile = UserProfile.Create(Guid.NewGuid(), "Display Two");
        secondProfile.Id = Guid.NewGuid();
        secondProfile.Visibility = ProfileVisibility.Public;
        secondProfile.CreatedAt = new DateTime(2024, 8, 3, 0, 0, 0, DateTimeKind.Utc);
        secondProfile.UpdatedAt = new DateTime(2024, 8, 4, 0, 0, 0, DateTimeKind.Utc);
        secondProfile.Version = 2;

        var query = new GetUserProfilesPagedQuery(
            Search: "display",
            SortBy: "displayName",
            SortDirection: "desc",
            PageNumber: 2,
            PageSize: 5);

        _profileRepositoryMock
            .Setup(x => x.GetProfilesPagedAsync("display", "displayName", "desc", 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserProfile> { firstProfile, secondProfile }, 7));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(7);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.Items[0].ProfileVisibility.Should().Be("friendsonly");
        result.Items[0].TimeZone.Should().BeNull();
        result.Items[0].Language.Should().BeNull();
        result.Items[0].ShowEmail.Should().BeFalse();
        result.Items[0].ShowLocation.Should().BeFalse();
        result.Items[0].AvatarUrl.Should().Be("https://example.com/avatar.png");
        result.Items[0].Version.Should().BeEquivalentTo(BitConverter.GetBytes(4));
        result.Items[1].ProfileVisibility.Should().Be("public");
    }
}