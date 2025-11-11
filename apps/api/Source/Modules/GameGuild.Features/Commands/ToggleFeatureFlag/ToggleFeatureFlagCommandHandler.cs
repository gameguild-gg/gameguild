using GameGuild.CQRS;

namespace GameGuild.Features.Commands.Handlers;

/// <summary>
///     Handler for ToggleFeatureFlagCommand
/// </summary>
public sealed class ToggleFeatureFlagCommandHandler : IRequestHandler<ToggleFeatureFlagCommand>
{
    // TODO: Inject repository/service dependencies

    public async Task<Unit> Handle(ToggleFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual toggle logic
        await Task.CompletedTask;

        return Unit.Value;
    }
}
