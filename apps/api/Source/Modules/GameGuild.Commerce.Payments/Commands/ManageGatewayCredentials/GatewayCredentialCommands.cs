using System.ComponentModel.DataAnnotations;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     HTTP request body for credential provisioning: carries only the plaintext value; the
///     provider and slot arrive on the route so they never duplicate (and the value is never
///     echoed back).
/// </summary>
/// <param name="Value">Plaintext credential value.</param>
public sealed record ProvisionGatewayCredentialRequest([Required, MaxLength(4096)] string Value);

/// <summary>
///     Provisions one payment-gateway API credential into the encrypted-at-rest store
///     (issue #413): the plaintext value is encrypted with ASP.NET DataProtection
///     (AES-256 under the application key ring) before it touches the database, and any
///     previously active credential of the same slot is deactivated atomically. Requires
///     the <c>payments:admin</c> permission; the acting administrator comes from the actor
///     context, never the request. The provider must be a registered gateway adapter —
///     credentials for unknown providers fail closed.
/// </summary>
/// <param name="ProviderId">Registered gateway provider identifier (stripe, paypal, authorizenet, braintree, adyen).</param>
/// <param name="SecretName">Credential slot on that gateway (api_key, client_id, client_secret, transaction_key, ...).</param>
/// <param name="Value">Plaintext credential value; never persisted, never returned, never logged.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record ProvisionGatewayCredentialCommand(
    [Required, MaxLength(50)] string ProviderId,
    [Required, MaxLength(100), RegularExpression("^[a-z0-9_]+$", ErrorMessage = "Secret names may contain lowercase letters, digits and underscores.")] string SecretName,
    [Required, MaxLength(4096)] string Value) : ICommand<GatewayCredentialProvisionResult>;

/// <summary>
///     Handler for <see cref="ProvisionGatewayCredentialCommand" />: resolves the acting
///     administrator from the actor context, verifies the target provider is a registered
///     gateway adapter (fail closed on unknown providers) and delegates to the encrypted
///     credential store.
/// </summary>
public sealed class ProvisionGatewayCredentialCommandHandler(
    IGatewayCredentialStore credentialStore,
    IGatewayRegistry gatewayRegistry,
    IActorContextAccessor actorContextAccessor,
    ILogger<ProvisionGatewayCredentialCommandHandler> logger)
    : ICommandHandler<ProvisionGatewayCredentialCommand, GatewayCredentialProvisionResult>
{
    /// <inheritdoc />
    public async Task<GatewayCredentialProvisionResult> Handle(
        ProvisionGatewayCredentialCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SecretName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Value);

        if (gatewayRegistry.Find(request.ProviderId) is null)
        {
            // Fail closed: never accept credentials for a provider that no adapter owns.
            throw new InvalidOperationException(
                $"'{request.ProviderId}' is not a registered payment gateway; credentials can only be provisioned for registered adapters.");
        }

        var actor = actorContextAccessor.ActorContext;
        var provisionedByUserId = actor.SubjectIdAsGuid;

        var result = await credentialStore
            .ProvisionAsync(request.ProviderId, request.SecretName, request.Value, provisionedByUserId, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Gateway credential {ProviderKey}/{SecretName} provisioned by {ProvisionedBy}",
            result.ProviderKey,
            result.SecretName,
            provisionedByUserId is { } userId ? userId.ToString() : "system");

        return result;
    }
}

/// <summary>
///     Deactivates the active credential of one (provider, secret name) slot (issue #413).
///     The encrypted row stays for audit; reads of the slot resolve to "no credential"
///     afterwards so callers fail closed to their explicit fallback. Requires the
///     <c>payments:admin</c> permission.
/// </summary>
/// <param name="ProviderId">Registered gateway provider identifier.</param>
/// <param name="SecretName">Credential slot to deactivate.</param>
[AuthorizeRequest(PaymentsPermission.Keys.Admin)]
public sealed record DeactivateGatewayCredentialCommand(
    [Required, MaxLength(50)] string ProviderId,
    [Required, MaxLength(100)] string SecretName) : ICommand<GatewayCredentialDeactivationResult>;

/// <summary>Outcome of a credential deactivation.</summary>
/// <param name="ProviderKey">Gateway provider identifier (normalized lowercase).</param>
/// <param name="SecretName">Credential slot that was addressed (normalized lowercase).</param>
/// <param name="Deactivated">Whether an active credential was deactivated.</param>
/// <param name="DeactivatedAtUtc">UTC instant of the deactivation; null when nothing was active.</param>
public sealed record GatewayCredentialDeactivationResult(
    string ProviderKey,
    string SecretName,
    bool Deactivated,
    DateTime? DeactivatedAtUtc);

/// <summary>
///     Handler for <see cref="DeactivateGatewayCredentialCommand" />.
/// </summary>
public sealed class DeactivateGatewayCredentialCommandHandler(
    IGatewayCredentialStore credentialStore,
    IActorContextAccessor actorContextAccessor)
    : ICommandHandler<DeactivateGatewayCredentialCommand, GatewayCredentialDeactivationResult>
{
    /// <inheritdoc />
    public async Task<GatewayCredentialDeactivationResult> Handle(
        DeactivateGatewayCredentialCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SecretName);

        var actor = actorContextAccessor.ActorContext;
        var deactivatedByUserId = actor.SubjectIdAsGuid;
        var deactivatedAtUtc = SystemClock.UtcNow;

        var deactivated = await credentialStore
            .DeactivateAsync(request.ProviderId, request.SecretName, deactivatedByUserId, cancellationToken)
            .ConfigureAwait(false);

        return new GatewayCredentialDeactivationResult(
            request.ProviderId.ToLowerInvariant(),
            request.SecretName.ToLowerInvariant(),
            deactivated,
            deactivated ? deactivatedAtUtc : null);
    }
}
