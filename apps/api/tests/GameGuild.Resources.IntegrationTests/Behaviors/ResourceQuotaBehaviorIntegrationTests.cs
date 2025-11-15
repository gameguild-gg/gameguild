using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Permissions.Abstractions;
using GameGuild.Resources.Attributes;
using GameGuild.Resources.Exceptions;
using GameGuild.Resources.Models;
using GameGuild.Resources.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Resources.IntegrationTests.Behaviors;

/// <summary>
/// Integration tests for ResourceQuotaBehavior end-to-end validation
/// </summary>
public class ResourceQuotaBehaviorIntegrationTests : IClassFixture<ResourceQuotaTestFixture>
{
    private readonly ResourceQuotaTestFixture _fixture;

    public ResourceQuotaBehaviorIntegrationTests(ResourceQuotaTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteCommand_WithinQuota_ShouldSucceed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        // Set up quota
        await quotaService!.SetQuotaAsync(
            tenantId,
            ResourceUsageType.Users,
            softLimit: 80,
            hardLimit: 100,
            ResourceQuotaPeriod.Monthly
        );

        // Act
        var command = new TestCreateUserCommand("test@example.com", "Test User");
        var result = await sender.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().NotBeEmpty();

        // Verify usage was recorded
        var currentUsage = await quotaService.GetCurrentUsageAsync(tenantId, ResourceUsageType.Users);
        currentUsage.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteCommand_ExceedsHardLimit_ShouldThrowException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        // Set up quota at the limit
        await quotaService!.SetQuotaAsync(
            tenantId,
            ResourceUsageType.Users,
            softLimit: 8,
            hardLimit: 10,
            ResourceQuotaPeriod.Monthly
        );

        // Create 10 users (reach the limit)
        for (int i = 0; i < 10; i++)
        {
            var cmd = new TestCreateUserCommand($"user{i}@example.com", $"User {i}");
            await sender.Send(cmd);
        }

        // Act - Try to create 11th user
        var command = new TestCreateUserCommand("user11@example.com", "User 11");
        Func<Task> act = async () => await sender.Send(command);

        // Assert
        await act.Should().ThrowAsync<QuotaExceededException>()
            .Where(e => e.ResourceType == ResourceUsageType.Users)
            .Where(e => e.TenantId == tenantId)
            .Where(e => e.CurrentUsage >= 10)
            .Where(e => e.Limit == 10);
    }

    [Fact]
    public async Task ExecuteCommand_WithoutQuotaConfigured_ShouldSucceed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);

        // Don't configure any quota

        // Act
        var command = new TestCreateUserCommand("test@example.com", "Test User");
        var result = await sender.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteMultipleCommands_ShouldAccumulateUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        await quotaService!.SetQuotaAsync(
            tenantId,
            ResourceUsageType.Users,
            softLimit: 40,
            hardLimit: 50,
            ResourceQuotaPeriod.Monthly
        );

        // Act - Create 5 users
        for (int i = 0; i < 5; i++)
        {
            var command = new TestCreateUserCommand($"user{i}@example.com", $"User {i}");
            await sender.Send(command);
        }

        // Assert
        var currentUsage = await quotaService.GetCurrentUsageAsync(tenantId, ResourceUsageType.Users);
        currentUsage.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteCommand_WithCustomAmount_ShouldRecordCorrectUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        await quotaService!.SetQuotaAsync(
            tenantId,
            ResourceUsageType.Storage,
            softLimit: 9000,
            hardLimit: 10000,
            ResourceQuotaPeriod.Monthly
        );

        // Act
        var command = new TestUploadFileCommand("file.txt", 2048); // 2KB
        var result = await sender.Send(command);

        // Assert
        result.Should().NotBeNull();

        var currentUsage = await quotaService.GetCurrentUsageAsync(tenantId, ResourceUsageType.Storage);
        currentUsage.Should().Be(2048);
    }

    [Fact]
    public async Task ExecuteCommand_WithRecordUsageFalse_ShouldNotRecordUsage()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        await quotaService!.SetQuotaAsync(
            tenantId,
            ResourceUsageType.Projects,
            softLimit: 80,
            hardLimit: 100,
            ResourceQuotaPeriod.Monthly
        );

        // Act
        var command = new TestCheckProjectCommand("project-1");
        await sender.Send(command);

        // Assert
        var currentUsage = await quotaService.GetCurrentUsageAsync(tenantId, ResourceUsageType.Projects);
        currentUsage.Should().Be(0); // No usage recorded
    }

    [Fact]
    public async Task ExecuteCommand_WithEnforceFalse_ShouldNotBlockExecution()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        // Set quota at limit
        await quotaService!.SetQuotaAsync(
            tenantId,
            ResourceUsageType.ApiCalls,
            softLimit: 5,
            hardLimit: 10,
            ResourceQuotaPeriod.Daily
        );

        // Create 10 API calls (reach limit)
        for (int i = 0; i < 10; i++)
        {
            var cmd = new TestApiCallCommand($"endpoint-{i}");
            await sender.Send(cmd);
        }

        // Act - 11th call should still succeed (EnforceHardLimit = false)
        var command = new TestApiCallCommand("endpoint-11");
        var result = await sender.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        var currentUsage = await quotaService.GetCurrentUsageAsync(tenantId, ResourceUsageType.ApiCalls);
        currentUsage.Should().Be(11);
    }

    [Fact]
    public async Task GetUsageHistory_AfterCommands_ShouldReturnRecords()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var sender = _fixture.GetCommandSender(tenantId);
        var quotaService = _fixture.GetService<IResourceQuotaService>();

        // Act - Create 3 users
        for (int i = 0; i < 3; i++)
        {
            var command = new TestCreateUserCommand($"user{i}@example.com", $"User {i}");
            await sender.Send(command);
        }

        // Assert
        var history = await quotaService!.GetUsageHistoryAsync(
            tenantId,
            ResourceUsageType.Users,
            fromDate: DateTime.UtcNow.AddHours(-1)
        );

        history.Should().HaveCount(3);
        history.Should().OnlyContain(r => r.TenantId == tenantId);
        history.Should().OnlyContain(r => r.Type == ResourceUsageType.Users);
        history.Should().OnlyContain(r => r.UsageAmount == 1);
    }
}

// Test Commands
[RequiresQuota(ResourceUsageType.Users, 1, Source = "TestCreateUser")]
public record TestCreateUserCommand(string Email, string Name) : ICommand<TestCreateUserResponse>;

public record TestCreateUserResponse(Guid UserId);

[RequiresQuota(ResourceUsageType.Storage, Source = "TestUploadFile")]
public record TestUploadFileCommand(string FileName, long FileSize) : ICommand<TestUploadFileResponse>;

public record TestUploadFileResponse(Guid FileId);

[RequiresQuota(ResourceUsageType.Projects, 1, RecordUsage = false)]
public record TestCheckProjectCommand(string ProjectId) : ICommand<TestCheckProjectResponse>;

public record TestCheckProjectResponse(bool Exists);

[RequiresQuota(ResourceUsageType.ApiCalls, 1, EnforceHardLimit = false, Source = "TestApiCall")]
public record TestApiCallCommand(string Endpoint) : ICommand<TestApiCallResponse>;

public record TestApiCallResponse(bool Success);

// Test Handlers
public class TestCreateUserCommandHandler : ICommandHandler<TestCreateUserCommand, TestCreateUserResponse>
{
    public Task<TestCreateUserResponse> Handle(TestCreateUserCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestCreateUserResponse(Guid.NewGuid()));
    }
}

public class TestUploadFileCommandHandler : ICommandHandler<TestUploadFileCommand, TestUploadFileResponse>
{
    public Task<TestUploadFileResponse> Handle(TestUploadFileCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestUploadFileResponse(Guid.NewGuid()));
    }
}

public class TestCheckProjectCommandHandler : ICommandHandler<TestCheckProjectCommand, TestCheckProjectResponse>
{
    public Task<TestCheckProjectResponse> Handle(TestCheckProjectCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestCheckProjectResponse(true));
    }
}

public class TestApiCallCommandHandler : ICommandHandler<TestApiCallCommand, TestApiCallResponse>
{
    public Task<TestApiCallResponse> Handle(TestApiCallCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestApiCallResponse(true));
    }
}
