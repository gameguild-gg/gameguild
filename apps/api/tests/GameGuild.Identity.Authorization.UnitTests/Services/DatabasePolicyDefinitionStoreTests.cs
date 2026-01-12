using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class DatabasePolicyDefinitionStoreTests
{
    [Fact]
    public async Task GetPolicyAsync_MapsPolicyFields()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        var entity = new PolicyDefinitionEntity
        {
            PolicyName = "Test",
            RequireAuthentication = true,
            AuthenticationSchemesJson = "[\"bearer\"]",
            RequiredPermissionsJson = "[\"p1\"]",
            RequiredRolesJson = "[\"admin\"]",
            RequireAccessControlListAccess = true,
            ResourceType = "Project",
            MinimumAccessLevel = "Write",
            IsTenantScoped = true,
            PolicyVersion = 3,
            UseRuleBasedEvaluation = true,
            RulesJson = """
            [
                {
                    "Type": "TenantMatch",
                    "Description": "Match tenant",
                    "Enabled": true
                }
            ]
            """
        };
        repo.Setup(r => r.GetByNameAsync("Test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var store = new DatabasePolicyDefinitionStore(repo.Object);

        var policy = await store.GetPolicyAsync("Test", null, CancellationToken.None);

        policy.Should().NotBeNull();
        policy!.RequiredPermissions.Should().Contain("p1");
        policy.RequiredRoles.Should().Contain("admin");
        policy.RequireAccessControlListAccess.Should().BeTrue();
        policy.UseRuleBasedEvaluation.Should().BeTrue();
        policy.Rules.Should().HaveCount(1);
        policy.Rules![0].Type.Should().Be("TenantMatch");
    }

    [Fact]
    public async Task GetTenantPoliciesAsync_ReturnsMappedPolicies()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PolicyDefinitionEntity>
            {
                new() { PolicyName = "One", RequiredPermissionsJson = "[\"p1\"]" }
            });

        var store = new DatabasePolicyDefinitionStore(repo.Object);
        var policies = await store.GetTenantPoliciesAsync(Guid.NewGuid().ToString(), CancellationToken.None);

        policies.Should().HaveCount(1);
        policies[0].RequiredPermissions.Should().ContainSingle().Which.Should().Be("p1");
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsMaxVersion()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PolicyDefinitionEntity>
            {
                new() { PolicyName = "v1", PolicyVersion = 1 },
                new() { PolicyName = "v2", PolicyVersion = 5 }
            });

        var store = new DatabasePolicyDefinitionStore(repo.Object);
        var version = await store.GetVersionAsync(Guid.NewGuid().ToString(), CancellationToken.None);

        version.Should().Be(5);
    }
}
