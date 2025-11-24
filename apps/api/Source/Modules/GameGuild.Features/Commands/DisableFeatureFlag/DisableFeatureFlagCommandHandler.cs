using GameGuild.CQRS;

namespace GameGuild.Features.Commands.Handlers;

/// <summary>
///     Handler for DisableFeatureFlagCommand
/// </summary>
public sealed class DisableFeatureFlagCommandHandler : IRequestHandler<DisableFeatureFlagCommand>
{
    // TODO: Inject repository/service dependencies

    public async Task<Unit> Handle(DisableFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual disable logic
        await Task.CompletedTask;

        return Unit.Value;
    }
}
