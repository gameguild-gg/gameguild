using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public record TestAbacExpressionCommand : ICommand<AbacExpressionTestResult>
{
    public string JsonExpression { get; init; } = string.Empty;

    public AbacEvaluationContext TestContext { get; init; } = new AbacEvaluationContext();
}
