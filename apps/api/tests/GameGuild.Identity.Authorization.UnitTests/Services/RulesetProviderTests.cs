using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class RulesetProviderTests
{
    private static RulesetProvider CreateProvider(Mock<IPolicyDefinitionRepository> repoMock)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new RulesetProvider(repoMock.Object, memoryCache, NullLogger<RulesetProvider>.Instance);
    }

    [Fact]
    public async Task GetRulesetAsync_ReturnsNull_WhenPolicyMissing()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync("Missing", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyDefinitionEntity?)null);
        var provider = CreateProvider(repo);

        var result = await provider.GetRulesetAsync("Missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRulesetAsync_UsesRuleBasedEvaluation_WhenRulesJsonPresent()
    {
        var rulesJson = JsonSerializer.Serialize(new[]
        {
            new RuleDefinition { Type = RuleTypes.RequireMfa, Enabled = true }
        });
        var entity = new PolicyDefinitionEntity
        {
            PolicyName = "RuleBased",
            RequireAuthentication = true,
            RulesJson = rulesJson,
            UseRuleBasedEvaluation = true,
            IsActive = true
        };

        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync("RuleBased", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var provider = CreateProvider(repo);

        var result = await provider.GetRulesetAsync("RuleBased", CancellationToken.None);

        result.Should().NotBeNull();
        result!.RequireAuthentication.Should().BeTrue();
        result.Rules.Should().ContainSingle(r => r.Type == RuleTypes.RequireMfa);
    }

    [Fact]
    public async Task GetRulesetAsync_ConvertsPermissionsToRules_WhenNoExplicitRules()
    {
        var entity = new PolicyDefinitionEntity
        {
            PolicyName = "PermissionBased",
            RequireAuthentication = true,
            RequiredPermissionsJson = "[\"p1\", \"p2\"]",
            RequiredRolesJson = "[\"admin\"]",
            RequireAccessControlListAccess = true,
            MinimumAccessLevel = "Write",
            UseRuleBasedEvaluation = true,
            RulesJson = null, // No explicit rules
            IsActive = true
        };

        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync("PermissionBased", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var provider = CreateProvider(repo);

        var result = await provider.GetRulesetAsync("PermissionBased", CancellationToken.None);

        result.Should().NotBeNull();
        var types = result!.Rules.Select(r => r.Type).ToList();
        types.Should().Contain(RuleTypes.RequireAllPermissions);
        types.Should().Contain(RuleTypes.OwnerOrAcl);
    }

    [Fact]
    public async Task GetRulesetAsync_UsesCacheUntilInvalidated()
    {
        var entity = new PolicyDefinitionEntity { PolicyName = "Cached", UseRuleBasedEvaluation = true, RulesJson = "[]", IsActive = true };
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync("Cached", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var provider = CreateProvider(repo);

        await provider.GetRulesetAsync("Cached", CancellationToken.None);
        await provider.GetRulesetAsync("Cached", CancellationToken.None);

        repo.Verify(r => r.GetByNameAsync("Cached", null, It.IsAny<CancellationToken>()), Times.Once);

        provider.InvalidatePolicy("Cached");
        await provider.GetRulesetAsync("Cached", CancellationToken.None);

        repo.Verify(r => r.GetByNameAsync("Cached", null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetRulesetAsync_InvalidRulesJson_DoesNotThrow()
    {
        var entity = new PolicyDefinitionEntity
        {
            PolicyName = "BadRules",
            RulesJson = "{invalid-json}",
            UseRuleBasedEvaluation = true,
            IsActive = true
        };

        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync("BadRules", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var provider = CreateProvider(repo);

        var result = await provider.GetRulesetAsync("BadRules", CancellationToken.None);

        result.Should().NotBeNull();
    }
}
