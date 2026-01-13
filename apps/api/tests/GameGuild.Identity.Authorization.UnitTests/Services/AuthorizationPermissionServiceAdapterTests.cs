using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class AuthorizationPermissionServiceAdapterTests
{
    private readonly Mock<IPermissionQueryService> _queryServiceMock = new();
    private readonly AuthorizationPermissionServiceAdapter _adapter;

    public AuthorizationPermissionServiceAdapterTests()
    {
        _adapter = new AuthorizationPermissionServiceAdapter(_queryServiceMock.Object);
    }

    [Fact]
    public async Task HasPermissionAsync_DelegatesToPermissionService()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _queryServiceMock
            .Setup(p => p.HasTenantPermissionAsync(userId, tenantId, "perm", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _adapter.HasPermissionAsync(userId, tenantId, "perm", CancellationToken.None);

        result.Should().BeTrue();
        _queryServiceMock.Verify(p => p.HasTenantPermissionAsync(userId, tenantId, "perm", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HasAllPermissionsAsync_NoPermissions_ReturnsAllPresent()
    {
        var result = await _adapter.HasAllPermissionsAsync(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<string>(), CancellationToken.None);

        result.HasAllRequired.Should().BeTrue();
        result.HasAnyRequired.Should().BeTrue();
        result.PresentPermissions.Should().BeEmpty();
        result.MissingPermissions.Should().BeEmpty();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_AllPresent_ReturnsAllPresent()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { "p1", "p2" };
        _queryServiceMock
            .Setup(p => p.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "p1", "p2", "extra" });

        var result = await _adapter.HasAllPermissionsAsync(userId, tenantId, permissions, CancellationToken.None);

        result.HasAllRequired.Should().BeTrue();
        result.MissingPermissions.Should().BeEmpty();
        result.PresentPermissions.Should().BeEquivalentTo("p1", "p2");
    }

    [Fact]
    public async Task HasAllPermissionsAsync_Partial_ReturnsPartial()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { "p1", "p2" };
        _queryServiceMock
            .Setup(p => p.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "p1" });

        var result = await _adapter.HasAllPermissionsAsync(userId, tenantId, permissions, CancellationToken.None);

        result.HasAllRequired.Should().BeFalse();
        result.HasAnyRequired.Should().BeTrue();
        result.PresentPermissions.Should().ContainSingle().Which.Should().Be("p1");
        result.MissingPermissions.Should().ContainSingle().Which.Should().Be("p2");
    }

    [Fact]
    public async Task HasAnyPermissionAsync_NoPermissions_ReturnsNonePresent()
    {
        var result = await _adapter.HasAnyPermissionAsync(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<string>(), CancellationToken.None);

        result.HasAnyRequired.Should().BeFalse();
        result.HasAllRequired.Should().BeFalse();
        result.PresentPermissions.Should().BeEmpty();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_SomePresent_ReturnsPartial()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var permissions = new[] { "p1", "p2" };
        _queryServiceMock
            .Setup(p => p.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "p2" });

        var result = await _adapter.HasAnyPermissionAsync(userId, tenantId, permissions, CancellationToken.None);

        result.HasAnyRequired.Should().BeTrue();
        result.HasAllRequired.Should().BeFalse();
        result.PresentPermissions.Should().ContainSingle().Which.Should().Be("p2");
        result.MissingPermissions.Should().ContainSingle().Which.Should().Be("p1");
    }

    [Fact]
    public async Task GetPermissionsAsync_DelegatesToPermissionService()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var granted = new List<string> { "a", "b" };
        _queryServiceMock
            .Setup(p => p.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(granted);

        var result = await _adapter.GetPermissionsAsync(userId, tenantId, CancellationToken.None);

        result.Should().BeEquivalentTo(granted);
        _queryServiceMock.Verify(p => p.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
