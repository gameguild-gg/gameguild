using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Abstractions;

/// <summary>
/// Unit tests for ScopedRuleEvaluatorFactory
/// </summary>
public class ScopedRuleEvaluatorFactoryTests
{
    [Fact]
    public void GetEvaluator_WithRegisteredType_ReturnsEvaluator()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Register dependencies for TenantMatchRuleEvaluator
        services.AddScoped(_ => new Mock<IAuthorizationTenantContext>().Object);
        services.AddScoped<TenantMatchRuleEvaluator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ScopedRuleEvaluatorFactory(serviceProvider);

        // Act
        var evaluator = factory.GetEvaluator(RuleTypes.TenantMatch);

        // Assert
        evaluator.Should().NotBeNull();
        evaluator.Should().BeOfType<TenantMatchRuleEvaluator>();
    }

    [Fact]
    public void GetEvaluator_WithUnregisteredType_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ScopedRuleEvaluatorFactory(serviceProvider);

        // Act
        var evaluator = factory.GetEvaluator("UnknownRuleType");

        // Assert
        evaluator.Should().BeNull();
    }

    [Fact]
    public void GetRegisteredTypes_ReturnsAllMappedTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ScopedRuleEvaluatorFactory(serviceProvider);

        // Act
        var types = factory.GetRegisteredTypes().ToList();

        // Assert
        types.Should().Contain(RuleTypes.TenantMatch);
        types.Should().Contain(RuleTypes.RequireAllPermissions);
        types.Should().Contain(RuleTypes.RequireAnyPermission);
        types.Should().Contain(RuleTypes.SelfOrPermission);
        types.Should().Contain(RuleTypes.OwnerOrAcl);
        types.Should().Contain(RuleTypes.RequireIpAllowList);
        types.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void GetAllMappings_ReturnsAllRuleTypeEvaluatorPairs()
    {
        // Act
        var mappings = ScopedRuleEvaluatorFactory.GetAllMappings().ToList();

        // Assert
        mappings.Should().NotBeEmpty();
        mappings.Should().Contain(m => m.RuleType == RuleTypes.TenantMatch);
        mappings.Should().Contain(m => m.RuleType == RuleTypes.RequireAllPermissions);
        mappings.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.RuleType));
        mappings.Should().OnlyContain(m => m.EvaluatorType != null);
    }

    [Fact]
    public void GetEvaluator_IsCaseInsensitive()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Register dependencies
        services.AddScoped(_ => new Mock<IAuthorizationTenantContext>().Object);
        services.AddScoped<TenantMatchRuleEvaluator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = new ScopedRuleEvaluatorFactory(serviceProvider);

        // Act
        var evaluator1 = factory.GetEvaluator("TenantMatch");
        var evaluator2 = factory.GetEvaluator("tenantmatch");
        var evaluator3 = factory.GetEvaluator("TENANTMATCH");

        // Assert
        evaluator1.Should().NotBeNull();
        evaluator2.Should().NotBeNull();
        evaluator3.Should().NotBeNull();
        
        evaluator1.Should().BeOfType<TenantMatchRuleEvaluator>();
    }

    [Fact]
    public void GetEvaluator_WithModuleRegistration_ResolvesExternalEvaluator()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExternalRuleEvaluator>(_ => new ExternalRuleEvaluator());
        var serviceProvider = services.BuildServiceProvider();
        var registrations = new[]
        {
            new ScopedRuleEvaluatorRegistration("External", typeof(ExternalRuleEvaluator))
        };
        var factory = new ScopedRuleEvaluatorFactory(serviceProvider, registrations);

        var evaluator = factory.GetEvaluator("external");

        evaluator.Should().BeOfType<ExternalRuleEvaluator>();
        factory.GetRegisteredTypes().Should().Contain("External");
    }

    private sealed class ExternalRuleEvaluator : IRuleEvaluator
    {
        public string RuleType => "External";

        public Task<RuleEvaluationResult> EvaluateAsync(
            AuthorizationHandlerContext context,
            RuleParameters parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RuleEvaluationResult.Success());
        }
    }
}
