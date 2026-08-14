using GameGuild.Assets.Security;

namespace GameGuild.Assets.UnitTests.Services;

public sealed class AssetUploadAuthorizationServiceTests
{
    [Fact]
    public async Task CanUploadAsync_UnknownParentType_IsDenied()
    {
        var service = new AssetUploadAuthorizationService(
            Mock.Of<IApplicationDbContext>(),
            []);

        var result = await service.CanUploadAsync(
            "Unknown",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUploadAsync_UsesParentManageAuthority()
    {
        var resolver = new Mock<IAssetParentAuthorizationResolver>();
        resolver.Setup(candidate => candidate.Supports("Project")).Returns(true);
        resolver.Setup(candidate => candidate.CanManageAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new AssetUploadAuthorizationService(
            Mock.Of<IApplicationDbContext>(),
            [resolver.Object]);

        var result = await service.CanUploadAsync(
            "Project",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Should().BeTrue();
        resolver.Verify(candidate => candidate.CanManageAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanUploadAsync_FolderWithoutParent_IsDenied()
    {
        var service = new AssetUploadAuthorizationService(
            Mock.Of<IApplicationDbContext>(),
            []);

        var result = await service.CanUploadAsync(
            null,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        result.Should().BeFalse();
    }
}
