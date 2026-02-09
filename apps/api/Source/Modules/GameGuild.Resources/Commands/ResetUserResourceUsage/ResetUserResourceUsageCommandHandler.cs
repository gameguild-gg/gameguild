using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for resetting user resource usage
/// </summary>
public sealed class ResetUserResourceUsageCommandHandler(IUsageRecordRepository usageRecordRepository) : ICommandHandler<ResetUserResourceUsageCommand>
{
    public async Task<Unit> Handle(ResetUserResourceUsageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await usageRecordRepository.DeleteByUserAndTypeAsync(request.UserId, request.ResourceUsageType, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
