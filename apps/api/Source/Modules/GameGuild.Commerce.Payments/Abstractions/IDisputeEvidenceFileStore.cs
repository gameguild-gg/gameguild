namespace GameGuild.Commerce.Payments;

/// <summary>
///     Resolves the authorized bytes of a stored dispute-evidence file (issue #419 provider
///     file delivery). Card networks do not review external download links, so a dispute
///     challenge must deliver the actual stored content through the provider's native
///     file/evidence fields; this seam is how the payment path obtains that content without
///     depending on a specific storage provider. Implementations must only resolve content the
///     acting context is authorized to deliver and return null when the stored bytes are
///     unavailable — a missing file degrades that item to its textual reference, never to a
///     fabricated upload.
/// </summary>
public interface IDisputeEvidenceFileStore
{
    /// <summary>
    ///     Resolves the stored content of <paramref name="evidence"/> for provider delivery.
    ///     Returns null when the row has no file content or the bytes cannot be read.
    /// </summary>
    Task<GatewayDisputeEvidenceContent?> ResolveAsync(DisputeEvidence evidence, CancellationToken cancellationToken = default);
}

/// <summary>
///     Fail-closed default (issue #419): with no evidence file store configured, stored file
///     bytes are unavailable and evidence items degrade to their textual references instead of
///     pretending a provider upload happened.
/// </summary>
public sealed class NullDisputeEvidenceFileStore : IDisputeEvidenceFileStore
{
    /// <inheritdoc />
    public Task<GatewayDisputeEvidenceContent?> ResolveAsync(DisputeEvidence evidence, CancellationToken cancellationToken = default) =>
        Task.FromResult<GatewayDisputeEvidenceContent?>(null);
}
