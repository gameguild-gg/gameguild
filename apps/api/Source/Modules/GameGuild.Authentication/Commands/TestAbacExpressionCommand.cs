using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Models.Abac;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record TestAbacExpressionCommand : ICommand<AbacExpressionTestResult>
{
    public string JsonExpression { get; init; } = string.Empty;

    public AbacEvaluationContext TestContext { get; init; } = new AbacEvaluationContext();
}
