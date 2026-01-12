using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Abstractions;

public class RuleEvaluatorRegistryTests
{
    [Fact]
    public void GetEvaluator_ReturnsRegisteredEvaluator()
    {
        var evaluator = new StubEvaluator("TestRule");
        var registry = new RuleEvaluatorRegistry(new[] { evaluator });

        var resolved = registry.GetEvaluator("TestRule");

        resolved.Should().BeSameAs(evaluator);
    }

    [Fact]
    public void GetEvaluator_IsCaseInsensitive()
    {
        var evaluator = new StubEvaluator("TestRule");
        var registry = new RuleEvaluatorRegistry(new[] { evaluator });

        registry.GetEvaluator("testrule").Should().NotBeNull();
        registry.GetEvaluator("TESTRULE").Should().BeSameAs(evaluator);
    }

    [Fact]
    public void GetRegisteredTypes_ReturnsAllKeys()
    {
        var evaluatorA = new StubEvaluator("RuleA");
        var evaluatorB = new StubEvaluator("RuleB");
        var registry = new RuleEvaluatorRegistry(new IRuleEvaluator[] { evaluatorA, evaluatorB });

        registry.GetRegisteredTypes().Should().BeEquivalentTo(new[] { "RuleA", "RuleB" });
    }

    private sealed class StubEvaluator(string ruleType) : IRuleEvaluator
    {
        public string RuleType { get; } = ruleType;

        public Task<RuleEvaluationResult> EvaluateAsync(
            AuthorizationHandlerContext context,
            RuleParameters parameters,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RuleEvaluationResult.Success());
        }
    }
}
