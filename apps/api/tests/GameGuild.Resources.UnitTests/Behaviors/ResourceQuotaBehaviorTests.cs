using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Attributes;
using GameGuild.Resources.Behaviors;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Exceptions;
using GameGuild.Resources.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Behaviors;

public class ResourceQuotaBehaviorTests
{
    private readonly Mock<IResourceQuotaService> _mockQuotaService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<ResourceQuotaBehavior<TestCommand, bool>>> _mockLogger;
    private readonly ResourceQuotaBehavior<TestCommand, bool> _behavior;
    private readonly Guid _testTenantId = Guid.NewGuid();

    public ResourceQuotaBehaviorTests()
    {
        _mockQuotaService = new Mock<IResourceQuotaService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<ResourceQuotaBehavior<TestCommand, bool>>>();

        _mockTenantContext.Setup(x => x.TenantId).Returns(_testTenantId);

        _behavior = new ResourceQuotaBehavior<TestCommand, bool>(
            _mockQuotaService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_WithoutRequiresQuotaAttribute_ShouldBypassValidation()
    {
        // Arrange
        var command = new TestCommandWithoutAttribute();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(x => x.CheckLimitsAsync(
            It.IsAny<Guid>(),
            It.IsAny<ResourceUsageType>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldBypassValidation()
    {
        // Arrange
        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);
        var command = new TestCommand();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(x => x.CheckLimitsAsync(
            It.IsAny<Guid>(),
            It.IsAny<ResourceUsageType>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithinQuota_ShouldExecuteCommandAndRecordUsage()
    {
        // Arrange
        var command = new TestCommand();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 5,
                HardLimit = 100
            });

        _mockQuotaService
            .Setup(x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Users, 1, null, "TestCommand", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Users, 1, null, "TestCommand", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExceedsHardLimit_WithEnforcement_ShouldThrowException()
    {
        // Arrange
        var command = new TestCommand();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = false,
                CurrentUsage = 100,
                HardLimit = 100,
                Message = "Hard limit exceeded"
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<QuotaExceededException>(
            async () => await _behavior.Handle(command, next, default));

        exception.ResourceType.Should().Be(ResourceUsageType.Users);
        exception.CurrentUsage.Should().Be(100);
        exception.Limit.Should().Be(100);

        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ExceedsHardLimit_WithoutEnforcement_ShouldLogWarningAndProceed()
    {
        // Arrange
        var command = new TestCommandWithoutEnforcement();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = false,
                CurrentUsage = 100,
                HardLimit = 100,
                Message = "Hard limit exceeded"
            });

        _mockQuotaService
            .Setup(x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Users, 1, null, "TestCommandWithoutEnforcement", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Users, 1, null, "TestCommandWithoutEnforcement", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomAmount_ShouldUseSpecifiedAmount()
    {
        // Arrange
        var command = new TestCommandWithCustomAmount();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Storage, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 500,
                HardLimit = 10000
            });

        _mockQuotaService
            .Setup(x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Storage, 1024, null, "TestCommandWithCustomAmount", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Storage, 1024, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Storage, 1024, null, "TestCommandWithCustomAmount", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithRecordUsageFalse_ShouldNotRecordUsage()
    {
        // Arrange
        var command = new TestCommandWithoutRecording();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 5,
                HardLimit = 100
            });

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenQuotaServiceThrows_ShouldLogAndProceed()
    {
        // Arrange
        var command = new TestCommand();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCustomSource_ShouldUseSpecifiedSource()
    {
        // Arrange
        var command = new TestCommandWithCustomSource();
        var next = new RequestHandlerDelegate<bool>(() => Task.FromResult(true));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.ApiCalls, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 50,
                HardLimit = 1000
            });

        _mockQuotaService
            .Setup(x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.ApiCalls, 1, null, "CustomSource", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _behavior.Handle(command, next, default);

        // Assert
        result.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.ApiCalls, 1, null, "CustomSource", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Test command classes
    [RequiresQuota(ResourceUsageType.Users)]
    private class TestCommand : ICommand<bool> { }

    private class TestCommandWithoutAttribute : ICommand<bool> { }

    [RequiresQuota(ResourceUsageType.Users, EnforceHardLimit = false)]
    private class TestCommandWithoutEnforcement : ICommand<bool> { }

    [RequiresQuota(ResourceUsageType.Storage, 1024)]
    private class TestCommandWithCustomAmount : ICommand<bool> { }

    [RequiresQuota(ResourceUsageType.Users, RecordUsage = false)]
    private class TestCommandWithoutRecording : ICommand<bool> { }

    [RequiresQuota(ResourceUsageType.ApiCalls, Source = "CustomSource")]
    private class TestCommandWithCustomSource : ICommand<bool> { }
}
