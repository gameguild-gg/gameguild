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
        var domainSeededPolicies = new[]
        {
            Policies.EmployeesRead,
            Policies.EmployeesCreate,
            Policies.EmployeesUpdate,
            Policies.EmployeesDelete
        };
        // The common seeder covers every platform policy and stays domain-free:
        // domain policies (including role-admission gates) arrive via IPolicySeedContributor.
        added.Select(policy => policy.PolicyName)
            .Should()
            .Contain(Policies.All.Except(domainSeededPolicies));
        added.Select(policy => policy.PolicyName).Should().NotContain(domainSeededPolicies);
        added.Should().NotContain(policy => policy.RulesJson != null && policy.RulesJson.Contains("PropertyManager"));
        added.Single(policy => policy.PolicyName == Policies.CourseContentPublicOutline)
            .RequireAuthentication.Should().BeFalse();
        added.Single(policy => policy.PolicyName == Policies.CourseContentLearner)
            .RulesJson.Should().Contain("\"access\": \"Learner\"");
        added.Single(policy => policy.PolicyName == Policies.CourseContentViewAll)
            .RulesJson.Should().Contain("\"allowCreator\": true");
        added.Single(policy => policy.PolicyName == Policies.CourseContentManage)
            .RulesJson.Should().Contain("\"allowCreator\": false");
        added.Single(policy => policy.PolicyName == Policies.UsersReadSelf)
            .RulesJson.Should().Contain("\"allowNoTenant\": true");
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_SeedsDomainPolicies_ThroughContributors()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyDefinitionEntity?)null);

        var added = new List<PolicyDefinitionEntity>();
        repo.Setup(r => r.AddAsync(It.IsAny<PolicyDefinitionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PolicyDefinitionEntity, CancellationToken>((p, _) => added.Add(p))
            .Returns(Task.CompletedTask);

        var contributor = new StubPolicySeedContributor();
        var seeder = new PolicyDefinitionSeeder(
            repo.Object,
            NullLogger<PolicyDefinitionSeeder>.Instance,
            [contributor]);

        await seeder.SeedAsync(CancellationToken.None);

        // Contributor policies are seeded with the same semantics as platform policies.
        added.Select(policy => policy.PolicyName).Should().Contain(StubPolicySeedContributor.DomainPolicyName);
        var domainPolicy = added.Single(policy => policy.PolicyName == StubPolicySeedContributor.DomainPolicyName);
        domainPolicy.PolicyVersion.Should().Be(PolicyDefinitionSeeder.CurrentPolicyVersion);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_DoesNotReseedContributorPolicies_AtCurrentVersion()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, Guid? _, CancellationToken _) =>
                new PolicyDefinitionEntity { PolicyName = name, PolicyVersion = PolicyDefinitionSeeder.CurrentPolicyVersion });

        var seeder = new PolicyDefinitionSeeder(
            repo.Object,
            NullLogger<PolicyDefinitionSeeder>.Instance,
            [new StubPolicySeedContributor()]);

        await seeder.SeedAsync(CancellationToken.None);

        repo.Verify(r => r.AddAsync(It.IsAny<PolicyDefinitionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class StubPolicySeedContributor : IPolicySeedContributor
    {
        public const string DomainPolicyName = "Stub.DomainPolicy";

        public string Name => "stub";

        public IEnumerable<PolicyDefinitionEntity> BuildPolicies()
        {
            yield return new PolicyDefinitionEntity
            {
                Id = Guid.NewGuid(),
                PolicyName = DomainPolicyName,
                Description = "Domain policy from a contributor",
                RequireAuthentication = true,
                RulesJson = "[]",
                IsActive = true
            };
        }
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
