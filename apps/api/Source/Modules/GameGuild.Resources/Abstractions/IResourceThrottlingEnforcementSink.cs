namespace GameGuild.Resources;

/// <summary>
///     Applies throttling decisions to the runtime request enforcement layer.
/// </summary>
public interface IResourceThrottlingEnforcementSink
{
    Task<ThrottlingEnforcementResult> ApplyAsync(
        Guid tenantId,
        ResourceUsageType type,
        long requestedAmount,
        ThrottlingResult decision,
        CancellationToken cancellationToken = default);
}
