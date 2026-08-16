using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class SoDServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    public void ParseConflictingPermissions_EmptyInputs_ReturnEmpty(string? raw)
    {
        ParseConflictingPermissions(raw).Should().BeEmpty();
    }

    [Fact]
    public void ParseConflictingPermissions_NormalizesJsonAndDelimitedFallback()
    {
        ParseConflictingPermissions("""[" ","read","READ"]""").Should().Equal("read");
        ParseConflictingPermissions("read, write;READ").Should().Equal("read", "write");
    }

    [Fact]
    public void HasPermissionConflict_WithFewerThanTwoConfiguredPermissions_ReturnsFalse()
    {
        var method = typeof(SoDService).GetMethod(
            "HasPermissionConflict",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var rule = new SoDRule { ConflictingPermissions = """["read"]""" };

        var result = (bool)method.Invoke(null, [rule, new[] { "read" }])!;

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DetectViolationsAsync_CreatesViolation_WhenEffectivePermissionsConflict()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var rule = CreateRule(tenantId);
        var ruleRepository = new Mock<ISoDRuleRepository>();
        var violationRepository = new Mock<ISoDViolationRepository>();
        var permissionQuery = new Mock<IPermissionQueryService>();

        ruleRepository.Setup(repo => repo.GetActiveRulesAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);
        ruleRepository.Setup(repo => repo.UpdateAsync(rule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        violationRepository.Setup(repo => repo.GetByUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        violationRepository.Setup(repo => repo.CreateAsync(It.IsAny<SoDViolation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SoDViolation violation, CancellationToken _) => violation);
        permissionQuery.Setup(query => query.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["payments:create", "payments:approve"]);

        var service = new SoDService(
            ruleRepository.Object,
            violationRepository.Object,
            NullLogger<SoDService>.Instance,
            permissionQuery.Object);

        var violations = await service.DetectViolationsAsync(userId, tenantId);

        violations.Should().ContainSingle();
        violations[0].RuleId.Should().Be(rule.Id);
        rule.ViolationCount.Should().Be(1);
        violationRepository.Verify(repo => repo.CreateAsync(It.IsAny<SoDViolation>(), It.IsAny<CancellationToken>()), Times.Once);
        ruleRepository.Verify(repo => repo.UpdateAsync(rule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DetectViolationsAsync_DoesNotDuplicateActiveViolation()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var rule = CreateRule(tenantId);
        var existingViolation = new SoDViolation
        {
            RuleId = rule.Id,
            UserId = userId,
            TenantId = tenantId,
            Status = SoDViolationStatus.Active
        };
        var ruleRepository = new Mock<ISoDRuleRepository>();
        var violationRepository = new Mock<ISoDViolationRepository>();
        var permissionQuery = new Mock<IPermissionQueryService>();

        ruleRepository.Setup(repo => repo.GetActiveRulesAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);
        violationRepository.Setup(repo => repo.GetByUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingViolation]);
        permissionQuery.Setup(query => query.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["payments:create", "payments:approve"]);

        var service = new SoDService(
            ruleRepository.Object,
            violationRepository.Object,
            NullLogger<SoDService>.Instance,
            permissionQuery.Object);

        var violations = await service.DetectViolationsAsync(userId, tenantId);

        violations.Should().ContainSingle().Which.Should().BeSameAs(existingViolation);
        violationRepository.Verify(repo => repo.CreateAsync(It.IsAny<SoDViolation>(), It.IsAny<CancellationToken>()), Times.Never);
        ruleRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SoDRule>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanForViolationsAsync_UsesTenantPermissionUsers()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var rule = CreateRule(tenantId);
        var ruleRepository = new Mock<ISoDRuleRepository>();
        var violationRepository = new Mock<ISoDViolationRepository>();
        var permissionQuery = new Mock<IPermissionQueryService>();
        var tenantPermissions = new Mock<ITenantPermissionRepository>();

        ruleRepository.Setup(repo => repo.GetActiveRulesAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);
        ruleRepository.Setup(repo => repo.UpdateAsync(rule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        tenantPermissions.Setup(repo => repo.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TenantPermission { UserId = userId, TenantId = tenantId, IsActive = true }
            ]);
        violationRepository.Setup(repo => repo.GetByUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        violationRepository.Setup(repo => repo.CreateAsync(It.IsAny<SoDViolation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SoDViolation violation, CancellationToken _) => violation);
        permissionQuery.Setup(query => query.GetEffectivePermissionsAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["payments:create", "payments:approve"]);

        var service = new SoDService(
            ruleRepository.Object,
            violationRepository.Object,
            NullLogger<SoDService>.Instance,
            permissionQuery.Object,
            tenantPermissions.Object);

        var count = await service.ScanForViolationsAsync(tenantId);

        count.Should().Be(1);
    }

    private static SoDRule CreateRule(Guid tenantId)
        => new()
        {
            TenantId = tenantId,
            Name = "Payment approval conflict",
            Description = "A user cannot both create and approve payments.",
            RuleType = SoDRuleType.PermissionConflict,
            IsEnabled = true,
            ConflictingPermissions = """["payments:create","payments:approve"]"""
        };

    private static string[] ParseConflictingPermissions(string? raw)
    {
        var method = typeof(SoDService).GetMethod(
            "ParseConflictingPermissions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (string[])method.Invoke(null, [raw])!;
    }
}
