using GameGuild.CQRS;

namespace GameGuild.Features.Commands.Handlers;

/// <summary>
///     Handler for EnableFeatureFlagCommand
/// </summary>
public sealed class EnableFeatureFlagCommandHandler : IRequestHandler<EnableFeatureFlagCommand>
{
    // TODO: Inject repository/service dependencies

    public async Task<Unit> Handle(EnableFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual enable logic
        await Task.CompletedTask;

        return Unit.Value;
    }
}
