using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class PolicyDefinitionSeederTests
{
    [Fact]
    public async Task SeedAsync_AddsMissingPolicies()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyDefinitionEntity?)null);

        var added = new List<PolicyDefinitionEntity>();
        repo.Setup(r => r.AddAsync(It.IsAny<PolicyDefinitionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PolicyDefinitionEntity, CancellationToken>((p, _) => added.Add(p))
            .Returns(Task.CompletedTask);

        var seeder = new PolicyDefinitionSeeder(repo.Object, NullLogger<PolicyDefinitionSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);

        added.Should().NotBeEmpty();
        added.Select(policy => policy.PolicyName).Should().Contain(Policies.All);
        added.Select(policy => policy.PolicyName).Should().Contain(new[]
        {
            Policies.EmployeesRead,
            Policies.EmployeesCreate,
            Policies.EmployeesUpdate,
            Policies.EmployeesDelete
        });
        added.Single(policy => policy.PolicyName == Policies.EmployeesCreate)
            .RulesJson.Should().Contain("users:create");
        added.Single(policy => policy.PolicyName == Policies.CourseContentPublicOutline)
            .RequireAuthentication.Should().BeFalse();
        added.Single(policy => policy.PolicyName == Policies.CourseContentLearner)
            .RulesJson.Should().Contain("\"access\": \"Learner\"");
        added.Single(policy => policy.PolicyName == Policies.CourseContentViewAll)
            .RulesJson.Should().Contain("\"allowCreator\": true");
        added.Single(policy => policy.PolicyName == Policies.CourseContentManage)
            .RulesJson.Should().Contain("\"allowCreator\": false");
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_DoesNothing_WhenPoliciesAreAtCurrentVersion()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, Guid? _, CancellationToken _) =>
                new PolicyDefinitionEntity { PolicyName = name, PolicyVersion = PolicyDefinitionSeeder.CurrentPolicyVersion });

        var seeder = new PolicyDefinitionSeeder(repo.Object, NullLogger<PolicyDefinitionSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);

        repo.Verify(r => r.AddAsync(It.IsAny<PolicyDefinitionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_ReconcilesCanonicalPoliciesFromOlderVersions()
    {
        var existing = new PolicyDefinitionEntity
        {
            PolicyName = Policies.UsersEditSelf,
            PolicyVersion = 1,
            RulesJson = "[]",
            RequireAuthentication = false
        };
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, Guid? _, CancellationToken _) =>
                name == Policies.UsersEditSelf
                    ? existing
                    : new PolicyDefinitionEntity
                    {
                        PolicyName = name,
                        PolicyVersion = PolicyDefinitionSeeder.CurrentPolicyVersion
                    });

        var seeder = new PolicyDefinitionSeeder(repo.Object, NullLogger<PolicyDefinitionSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);

        existing.PolicyVersion.Should().Be(PolicyDefinitionSeeder.CurrentPolicyVersion);
        existing.RequireAuthentication.Should().BeTrue();
        existing.RulesJson.Should().Contain(RuleTypes.SelfOrPermission);
        repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
