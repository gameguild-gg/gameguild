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
        added.Select(policy => policy.PolicyName).Should().Contain(new[]
        {
            Policies.EmployeesRead,
            Policies.EmployeesCreate,
            Policies.EmployeesUpdate,
            Policies.EmployeesDelete
        });
        added.Single(policy => policy.PolicyName == Policies.EmployeesCreate)
            .RulesJson.Should().Contain("users:create");
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_DoesNothing_WhenPoliciesExist()
    {
        var repo = new Mock<IPolicyDefinitionRepository>();
        repo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyDefinitionEntity { PolicyName = "Existing" });

        var seeder = new PolicyDefinitionSeeder(repo.Object, NullLogger<PolicyDefinitionSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);

        repo.Verify(r => r.AddAsync(It.IsAny<PolicyDefinitionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
