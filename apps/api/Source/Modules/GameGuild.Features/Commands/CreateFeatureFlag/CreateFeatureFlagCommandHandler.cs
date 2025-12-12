using GameGuild.CQRS;

namespace GameGuild.Features.Commands.Handlers;

/// <summary>
///     Handler for CreateFeatureFlagCommand
/// </summary>
public sealed class CreateFeatureFlagCommandHandler : IRequestHandler<CreateFeatureFlagCommand, Guid>
{
    // TODO: Inject repository/service dependencies

    public async Task<Guid> Handle(CreateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        // TODO: Implement actual create logic
        await Task.CompletedTask;

        return Guid.NewGuid();
    }
}
