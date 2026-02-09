using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record TestAbacExpressionCommand : ICommand<AbacExpressionTestResult>
{
    public string JsonExpression { get; init; } = string.Empty;

    public AbacEvaluationContext TestContext { get; init; } = new AbacEvaluationContext();
}
