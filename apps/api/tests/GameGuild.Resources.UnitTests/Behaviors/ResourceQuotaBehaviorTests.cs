using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Resources.Attributes;
using GameGuild.Resources.Behaviors;
using GameGuild.Resources.Exceptions;
using GameGuild.Resources.Models;
using GameGuild.Resources.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Resources.UnitTests.Behaviors;

/// <summary>
/// Unit tests for ResourceQuotaBehavior
/// </summary>
public class ResourceQuotaBehaviorTests
{
    private readonly Mock<IResourceQuotaService> _mockQuotaService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<ResourceQuotaBehavior<TestCommand, TestResponse>>> _mockLogger;
    private readonly ResourceQuotaBehavior<TestCommand, TestResponse> _behavior;
    private readonly Guid _testTenantId;

    public ResourceQuotaBehaviorTests()
    {
        _mockQuotaService = new Mock<IResourceQuotaService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<ResourceQuotaBehavior<TestCommand, TestResponse>>>();
        _testTenantId = Guid.NewGuid();

        _mockTenantContext.Setup(x => x.TenantId).Returns(_testTenantId);

        _behavior = new ResourceQuotaBehavior<TestCommand, TestResponse>(
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
        var expectedResponse = new TestResponse(Guid.NewGuid());
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        var behavior = new ResourceQuotaBehavior<TestCommandWithoutAttribute, TestResponse>(
            _mockQuotaService.Object,
            _mockTenantContext.Object,
            Mock.Of<ILogger<ResourceQuotaBehavior<TestCommandWithoutAttribute, TestResponse>>>()
        );

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldBypassValidation()
    {
        // Arrange
        var command = new TestCommand();
        var expectedResponse = new TestResponse(Guid.NewGuid());
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        _mockTenantContext.Setup(x => x.TenantId).Returns((Guid?)null);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();
        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithinQuota_ShouldExecuteCommandAndRecordUsage()
    {
        // Arrange
        var command = new TestCommand();
        var userId = Guid.NewGuid();
        var expectedResponse = new TestResponse(userId);
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 50,
                HardLimit = 100,
                SoftLimit = 80
            });

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();

        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Users, 1, userId, "TestCommand", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ExceedsSoftLimit_ShouldLogWarningButProceed()
    {
        // Arrange
        var command = new TestCommand();
        var userId = Guid.NewGuid();
        var expectedResponse = new TestResponse(userId);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 85,
                HardLimit = 100,
                SoftLimit = 80,
                WouldExceedSoftLimit = true
            });

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("soft limit")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ExceedsHardLimit_WithEnforcement_ShouldThrowException()
    {
        // Arrange
        var command = new TestCommand();

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(new TestResponse(Guid.NewGuid()));

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = false,
                CurrentUsage = 100,
                HardLimit = 100,
                SoftLimit = 80,
                WouldExceedHardLimit = true
            });

        // Act
        Func<Task> act = async () => await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<QuotaExceededException>()
            .Where(e => e.ResourceType == ResourceUsageType.Users)
            .Where(e => e.CurrentUsage == 100)
            .Where(e => e.Limit == 100)
            .Where(e => e.TenantId == _testTenantId);

        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ExceedsHardLimit_WithoutEnforcement_ShouldLogWarningAndProceed()
    {
        // Arrange
        var command = new TestCommandWithoutEnforcement();
        var userId = Guid.NewGuid();
        var expectedResponse = new TestResponse(userId);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        var behavior = new ResourceQuotaBehavior<TestCommandWithoutEnforcement, TestResponse>(
            _mockQuotaService.Object,
            _mockTenantContext.Object,
            Mock.Of<ILogger<ResourceQuotaBehavior<TestCommandWithoutEnforcement, TestResponse>>>()
        );

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.ApiCalls, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = false,
                CurrentUsage = 105,
                HardLimit = 100,
                WouldExceedHardLimit = true
            });

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_WithCustomAmount_ShouldUseSpecifiedAmount()
    {
        // Arrange
        var command = new TestCommandWithCustomAmount();
        var expectedResponse = new TestResponse(Guid.NewGuid());

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        var behavior = new ResourceQuotaBehavior<TestCommandWithCustomAmount, TestResponse>(
            _mockQuotaService.Object,
            _mockTenantContext.Object,
            Mock.Of<ILogger<ResourceQuotaBehavior<TestCommandWithCustomAmount, TestResponse>>>()
        );

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Storage, 2048, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 1000,
                HardLimit = 10000
            });

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);

        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Storage, 2048, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Storage, 2048, It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithRecordUsageFalse_ShouldNotRecordUsage()
    {
        // Arrange
        var command = new TestCommandWithoutRecording();
        var expectedResponse = new TestResponse(Guid.NewGuid());

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        var behavior = new ResourceQuotaBehavior<TestCommandWithoutRecording, TestResponse>(
            _mockQuotaService.Object,
            _mockTenantContext.Object,
            Mock.Of<ILogger<ResourceQuotaBehavior<TestCommandWithoutRecording, TestResponse>>>()
        );

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Projects, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 10,
                HardLimit = 100
            });

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);

        _mockQuotaService.Verify(
            x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Projects, 1, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(), It.IsAny<long>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenQuotaServiceThrows_ShouldLogAndProceed()
    {
        // Arrange
        var command = new TestCommand();
        var expectedResponse = new TestResponse(Guid.NewGuid());
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(expectedResponse);
        };

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        nextCalled.Should().BeTrue();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("quota check failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithCustomSource_ShouldUseSpecifiedSource()
    {
        // Arrange
        var command = new TestCommandWithCustomSource();
        var userId = Guid.NewGuid();
        var expectedResponse = new TestResponse(userId);

        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult(expectedResponse);

        var behavior = new ResourceQuotaBehavior<TestCommandWithCustomSource, TestResponse>(
            _mockQuotaService.Object,
            _mockTenantContext.Object,
            Mock.Of<ILogger<ResourceQuotaBehavior<TestCommandWithCustomSource, TestResponse>>>()
        );

        _mockQuotaService
            .Setup(x => x.CheckLimitsAsync(_testTenantId, ResourceUsageType.Users, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceLimitCheckResponse
            {
                CanProceed = true,
                CurrentUsage = 10,
                HardLimit = 100
            });

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        _mockQuotaService.Verify(
            x => x.RecordUsageAsync(_testTenantId, ResourceUsageType.Users, 1, userId, "ImportFromCsv", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    // Test command classes
    [RequiresQuota(ResourceUsageType.Users, 1, Source = "TestCommand")]
    private record TestCommand : ICommand<TestResponse>;

    private record TestCommandWithoutAttribute : ICommand<TestResponse>;

    [RequiresQuota(ResourceUsageType.ApiCalls, 1, EnforceHardLimit = false)]
    private record TestCommandWithoutEnforcement : ICommand<TestResponse>;

    [RequiresQuota(ResourceUsageType.Storage, 2048)]
    private record TestCommandWithCustomAmount : ICommand<TestResponse>;

    [RequiresQuota(ResourceUsageType.Projects, 1, RecordUsage = false)]
    private record TestCommandWithoutRecording : ICommand<TestResponse>;

    [RequiresQuota(ResourceUsageType.Users, 1, Source = "ImportFromCsv")]
    private record TestCommandWithCustomSource : ICommand<TestResponse>;

    private record TestResponse(Guid UserId);
}
