using FluentAssertions;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Context.Actors;
using Moq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class DataMaskingServiceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ApplyAsync_MasksConfiguredFieldsRecursivelyAndMatchesJsonNaming()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actor = ActorContextBuilder.ForUser(actorId).Build() with { TenantId = tenantId };
        var repository = new Mock<IDataMaskingRuleRepository>();
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule>());
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule>
            {
                CreateRule(tenantId, "Email", MaskingType.Redact),
                CreateRule(tenantId, "Profile.SSN", MaskingType.Partial, showLast: 4)
            });
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(actor);
        var service = new DataMaskingService(repository.Object, actorAccessor.Object);

        var masked = (JsonObject)(await service.ApplyAsync("User", new UserResponse
        {
            Email = "person@example.com",
            Profile = new ProfileResponse { Ssn = "123-45-6789" }
        }, SerializerOptions, CancellationToken.None))!;

        masked["email"]!.GetValue<string>().Should().Be("[REDACTED]");
        masked["profile"]!["ssn"]!.GetValue<string>().Should().Be("*******6789");
    }

    [Fact]
    public async Task ApplyAsync_TenantRuleOverridesGlobalRuleForTheSameField()
    {
        var tenantId = Guid.NewGuid();
        var actor = ActorContextBuilder.ForUser(Guid.NewGuid()).Build() with { TenantId = tenantId };
        var repository = new Mock<IDataMaskingRuleRepository>();
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule> { CreateRule(null, "Email", MaskingType.Redact, priority: 99) });
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule> { CreateRule(tenantId, "Email", MaskingType.Partial, showFirst: 2, showLast: 2) });
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(actor);
        var service = new DataMaskingService(repository.Object, actorAccessor.Object);

        var masked = (JsonObject)(await service.ApplyAsync("User", new { Email = "user@example.com" }, SerializerOptions))!;

        masked["email"]!.GetValue<string>().Should().Be("us" + new string('*', 12) + "om");
    }

    [Fact]
    public async Task ApplyAsync_UsesTheConfiguredApiNamingPolicy()
    {
        var repository = new Mock<IDataMaskingRuleRepository>();
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule> { CreateRule(null, "user_email", MaskingType.Redact) });
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(ActorContextBuilder.ForUser(Guid.NewGuid()).Build());
        var service = new DataMaskingService(repository.Object, actorAccessor.Object);
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var masked = (JsonObject)(await service.ApplyAsync(
            "User",
            new { UserEmail = "person@example.com" },
            serializerOptions))!;

        masked["user_email"]!.GetValue<string>().Should().Be("[REDACTED]");
    }

    [Fact]
    public async Task ApplyAsync_ExemptRoleReceivesOriginalUnmaskedValue()
    {
        var rule = CreateRule(null, "Email", MaskingType.Redact);
        rule.ExemptRoles = "[\"Support\"]";
        var repository = new Mock<IDataMaskingRuleRepository>();
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule> { rule });
        var actor = ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("support").Build();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(actor);
        var service = new DataMaskingService(repository.Object, actorAccessor.Object);
        var response = new { Email = "person@example.com" };

        var result = await service.ApplyAsync("User", response, SerializerOptions);

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task ApplyAsync_RequiredPermissionsAllowAnExplicitUnmaskedRead()
    {
        var rule = CreateRule(null, "Email", MaskingType.Redact);
        rule.RequiredPermissions = "[\"users:read-sensitive\"]";
        var repository = new Mock<IDataMaskingRuleRepository>();
        repository.Setup(repo => repo.GetByResourceTypeAsync("User", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataMaskingRule> { rule });
        var actor = ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithPermission("users:read-sensitive")
            .Build();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(actor);
        var service = new DataMaskingService(repository.Object, actorAccessor.Object);
        var response = new { Email = "person@example.com" };

        var result = await service.ApplyAsync("User", response, SerializerOptions);

        result.Should().BeSameAs(response);
    }

    private static DataMaskingRule CreateRule(
        Guid? tenantId,
        string fieldName,
        MaskingType type,
        int? showFirst = null,
        int? showLast = null,
        int priority = 0) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId.HasValue ? new TenantId(tenantId.Value) : null,
        Name = $"{fieldName} masking",
        ResourceType = "User",
        FieldName = fieldName,
        MaskingType = type,
        ShowFirst = showFirst,
        ShowLast = showLast,
        Priority = priority,
        IsEnabled = true
    };

    private sealed class UserResponse
    {
        public string Email { get; init; } = string.Empty;
        public ProfileResponse Profile { get; init; } = new();
    }

    private sealed class ProfileResponse
    {
        public string Ssn { get; init; } = string.Empty;
    }
}
