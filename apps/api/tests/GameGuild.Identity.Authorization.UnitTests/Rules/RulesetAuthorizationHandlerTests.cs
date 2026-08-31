using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Rules;

public class RulesetAuthorizationHandlerTests
{
    private static RulesetAuthorizationHandler CreateHandler(
        IRulesetProvider? provider = null,
        IRuleEvaluatorRegistry? registry = null,
        IScopedRuleEvaluatorFactory? factory = null)
    {
        return new RulesetAuthorizationHandler(
            provider ?? Mock.Of<IRulesetProvider>(),
            registry ?? Mock.Of<IRuleEvaluatorRegistry>(),
            factory ?? Mock.Of<IScopedRuleEvaluatorFactory>(),
            NullLogger<RulesetAuthorizationHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithRegistryEvaluator_Succeeds()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa);
        var requirement = new RulesetRequirement("TestPolicy", ruleset);
        var registryMock = new Mock<IRuleEvaluatorRegistry>();
        var evaluator = new StubEvaluator(RuleTypes.RequireMfa, RuleEvaluationResult.Success());
        registryMock.Setup(x => x.GetEvaluator(RuleTypes.RequireMfa)).Returns(evaluator);
        var handler = CreateHandler(registry: registryMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        context.HasFailed.Should().BeFalse();
        evaluator.Called.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithFactoryEvaluatorWhenRegistryMissing_Succeeds()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa);
        var requirement = new RulesetRequirement("TestPolicy", ruleset);
        var registryMock = new Mock<IRuleEvaluatorRegistry>();
        registryMock.Setup(x => x.GetEvaluator(It.IsAny<string>())).Returns((IRuleEvaluator?)null);
        var evaluator = new StubEvaluator(RuleTypes.RequireMfa, RuleEvaluationResult.Success());
        var factoryMock = new Mock<IScopedRuleEvaluatorFactory>();
        factoryMock.Setup(x => x.GetEvaluator(RuleTypes.RequireMfa)).Returns(evaluator);
        var handler = CreateHandler(registry: registryMock.Object, factory: factoryMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        evaluator.Called.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MissingRuleset_Fails()
    {
        var requirement = new RulesetRequirement("MissingPolicy", null);
        var providerMock = new Mock<IRulesetProvider>();
        providerMock.Setup(x => x.GetRulesetAsync("MissingPolicy", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyRuleset?)null);
        var handler = CreateHandler(provider: providerMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_InactiveRuleset_Fails()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa, isActive: false);
        var requirement = new RulesetRequirement("Inactive", ruleset);
        var handler = CreateHandler();
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_RequiresAuthenticationButUserUnauthenticated_Fails()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa, requireAuth: true);
        var requirement = new RulesetRequirement("Auth", ruleset);
        var handler = CreateHandler();
        var context = CreateContext(requirement, authenticated: false);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_InvalidRuleConfiguration_Fails()
    {
        var invalidRule = new RuleDefinition { Type = "UnknownType", Params = new Dictionary<string, System.Text.Json.JsonElement>() };
        var ruleset = new PolicyRuleset { Name = "Invalid", Rules = new[] { invalidRule } };
        var requirement = new RulesetRequirement("Invalid", ruleset);
        var handler = CreateHandler();
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MissingEvaluator_Fails()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa);
        var requirement = new RulesetRequirement("NoEval", ruleset);
        var registryMock = new Mock<IRuleEvaluatorRegistry>();
        registryMock.Setup(x => x.GetEvaluator(It.IsAny<string>())).Returns((IRuleEvaluator?)null);
        var factoryMock = new Mock<IScopedRuleEvaluatorFactory>();
        factoryMock.Setup(x => x.GetEvaluator(It.IsAny<string>())).Returns((IRuleEvaluator?)null);
        var handler = CreateHandler(registry: registryMock.Object, factory: factoryMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EvaluatorFailure_Fails()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa);
        var requirement = new RulesetRequirement("Fail", ruleset);
        var evaluator = new StubEvaluator(RuleTypes.RequireMfa, RuleEvaluationResult.Fail("boom"));
        var registryMock = new Mock<IRuleEvaluatorRegistry>();
        registryMock.Setup(x => x.GetEvaluator(RuleTypes.RequireMfa)).Returns(evaluator);
        var handler = CreateHandler(registry: registryMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EvaluatorThrows_Fails()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa);
        var requirement = new RulesetRequirement("Throw", ruleset);
        var evaluator = new StubEvaluator(RuleTypes.RequireMfa, exception: new InvalidOperationException("error"));
        var registryMock = new Mock<IRuleEvaluatorRegistry>();
        registryMock.Setup(x => x.GetEvaluator(RuleTypes.RequireMfa)).Returns(evaluator);
        var handler = CreateHandler(registry: registryMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_SkippedRequiredRule_FailsClosed()
    {
        var ruleset = CreateRuleset(RuleTypes.RequireMfa);
        var requirement = new RulesetRequirement("Skip", ruleset);
        var evaluator = new StubEvaluator(RuleTypes.RequireMfa, RuleEvaluationResult.Skip("skip"));
        var registryMock = new Mock<IRuleEvaluatorRegistry>();
        registryMock.Setup(x => x.GetEvaluator(RuleTypes.RequireMfa)).Returns(evaluator);
        var handler = CreateHandler(registry: registryMock.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EmptyRuleset_FailsClosed()
    {
        var ruleset = new PolicyRuleset
        {
            Name = "Empty",
            RequireAuthentication = true,
            Rules = []
        };
        var requirement = new RulesetRequirement("Empty", ruleset);
        var handler = CreateHandler();
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        context.HasFailed.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_AnyOf_SucceedsWhenOneChildRulePasses()
    {
        var first = new StubEvaluator(RuleTypes.RequireMfa, RuleEvaluationResult.Fail("no MFA"));
        var second = new StubEvaluator(RuleTypes.TenantMatch, RuleEvaluationResult.Success());
        var registry = new Mock<IRuleEvaluatorRegistry>();
        registry.Setup(candidate => candidate.GetEvaluator(RuleTypes.RequireMfa)).Returns(first);
        registry.Setup(candidate => candidate.GetEvaluator(RuleTypes.TenantMatch)).Returns(second);
        var anyOf = new RuleDefinition
        {
            Type = RuleTypes.AnyOf,
            Rules =
            [
                new RuleDefinition { Type = RuleTypes.RequireMfa },
                new RuleDefinition { Type = RuleTypes.TenantMatch }
            ]
        };
        var ruleset = new PolicyRuleset { Name = "AnyOf", Rules = [anyOf] };
        var requirement = new RulesetRequirement("AnyOf", ruleset);
        var handler = CreateHandler(registry: registry.Object);
        var context = CreateContext(requirement, authenticated: true);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        first.Called.Should().BeTrue();
        second.Called.Should().BeTrue();
    }

    private static AuthorizationHandlerContext CreateContext(
        RulesetRequirement requirement,
        bool authenticated)
    {
        var identity = authenticated ? new ClaimsIdentity("test") : new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        return new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);
    }

    private static PolicyRuleset CreateRuleset(
        string ruleType,
        bool requireAuth = false,
        bool isActive = true)
    {
        var rule = new RuleDefinition
        {
            Type = ruleType,
            Params = new Dictionary<string, System.Text.Json.JsonElement>()
        };

        return new PolicyRuleset
        {
            Name = "TestPolicy",
            RequireAuthentication = requireAuth,
            IsActive = isActive,
            Rules = new[] { rule }
        };
    }

    private sealed class StubEvaluator : IRuleEvaluator
    {
        private readonly RuleEvaluationResult? _result;
        private readonly Exception? _exception;

        public StubEvaluator(string ruleType, RuleEvaluationResult? result = null, Exception? exception = null)
        {
            RuleType = ruleType;
            _result = result;
            _exception = exception;
        }

        public string RuleType { get; }
        public bool Called { get; private set; }

        public Task<RuleEvaluationResult> EvaluateAsync(
            AuthorizationHandlerContext context,
            RuleParameters parameters,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_result ?? RuleEvaluationResult.Success());
        }
    }
}
