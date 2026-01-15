using FluentAssertions;
using GameGuild.Identity.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

/// <summary>
/// Unit tests for TenantMembershipChecker implementations.
/// Tests ensure proper tenant isolation and fail-closed behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Security", "TenantIsolation")]
public class TenantMembershipCheckerTests
{
    #region FailClosedTenantMembershipChecker Tests

    [Fact]
    public async Task FailClosedChecker_AlwaysReturnsFalse()
    {
        // Arrange
        var checker = new FailClosedTenantMembershipChecker();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var result = await checker.IsUserMemberOfTenantAsync(userId, tenantId);

        // Assert - Fail-closed: always deny
        result.Should().BeFalse();
    }

    [Fact]
    public async Task FailClosedChecker_ReturnsFalse_ForAnyInput()
    {
        // Arrange
        var checker = new FailClosedTenantMembershipChecker();

        // Act & Assert - Various inputs should all return false
        (await checker.IsUserMemberOfTenantAsync(Guid.Empty, Guid.Empty)).Should().BeFalse();
        (await checker.IsUserMemberOfTenantAsync(Guid.NewGuid(), Guid.Empty)).Should().BeFalse();
        (await checker.IsUserMemberOfTenantAsync(Guid.Empty, Guid.NewGuid())).Should().BeFalse();
        (await checker.IsUserMemberOfTenantAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeFalse();
    }

    #endregion

    #region TenantMembershipChecker Tests (requires mock repository)

    [Fact]
    public async Task TenantMembershipChecker_ReturnsFalse_ForNonMember()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        mockRepository.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act
        var result = await checker.IsUserMemberOfTenantAsync(userId, tenantId);

        // Assert
        result.Should().BeFalse();
        mockRepository.Verify(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TenantMembershipChecker_ReturnsTrue_ForActiveMember()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var activeMember = new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            IsActive = true
        };

        mockRepository.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeMember);

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act
        var result = await checker.IsUserMemberOfTenantAsync(userId, tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TenantMembershipChecker_ReturnsFalse_ForInactiveMember()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var inactiveMember = new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            IsActive = false
        };

        mockRepository.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveMember);

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act
        var result = await checker.IsUserMemberOfTenantAsync(userId, tenantId);

        // Assert - Inactive members should not have access
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TenantMembershipChecker_ReturnsFalse_ForDeactivatedMember()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var deactivatedMember = new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            IsActive = false,
            LeftAt = DateTime.UtcNow.AddDays(-1)
        };

        mockRepository.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deactivatedMember);

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act
        var result = await checker.IsUserMemberOfTenantAsync(userId, tenantId);

        // Assert - Deactivated members should not have access
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TenantMembershipChecker_HandlesEmptyGuid_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        mockRepository.Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act
        var result = await checker.IsUserMemberOfTenantAsync(Guid.Empty, Guid.Empty);

        // Assert - Empty GUIDs should return false
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TenantMembershipChecker_HandlesCancellation()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockRepository.Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => checker.IsUserMemberOfTenantAsync(Guid.NewGuid(), Guid.NewGuid(), cts.Token));
    }

    #endregion

    #region Cross-Tenant Isolation Tests

    [Fact]
    public async Task TenantMembershipChecker_DifferentTenant_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<ITenantMemberRepository>();
        var userId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // User is member of TenantA
        var memberOfA = new TenantMember { UserId = userId, TenantId = tenantA, IsActive = true };

        mockRepository.Setup(r => r.GetByUserAndTenantAsync(userId, tenantA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberOfA);
        mockRepository.Setup(r => r.GetByUserAndTenantAsync(userId, tenantB, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var checker = new TenantMembershipChecker(mockRepository.Object);

        // Act
        var resultA = await checker.IsUserMemberOfTenantAsync(userId, tenantA);
        var resultB = await checker.IsUserMemberOfTenantAsync(userId, tenantB);

        // Assert
        resultA.Should().BeTrue();
        resultB.Should().BeFalse(); // IDOR Prevention: Cannot access other tenant
    }

    #endregion
}
