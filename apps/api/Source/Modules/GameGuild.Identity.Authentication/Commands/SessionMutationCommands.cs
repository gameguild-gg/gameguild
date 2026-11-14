using System.Security.Cryptography;
using System.Text;
using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record TerminateSessionCommand(Guid SessionId, SessionTerminationReason Reason) : ICommand<bool>;
public sealed record TerminateUserSessionsCommand(
    Guid UserId,
    SessionTerminationReason Reason,
    Guid? ExceptSessionId = null) : ICommand<int>;
public sealed record RefreshUserSessionCommand(Guid SessionId) : ICommand<bool>;

/// <summary>
///     Trusts the device of the acting session. Trusting a long-lived session's device is a
///     security-sensitive grant (it satisfies the trusted-device policy for future sensitive
///     operations), so the command requires a fresh step-up receipt (issues #1144 and #1191).
///     Without a receipt, the endpoint returns a persisted challenge bound to operation type
///     <c>trust-device</c>, target reference <c>current-session</c>, and the SHA-256 hash of the
///     trimmed device name. The client verifies an enrolled supported factor and retries with
///     the returned receipt.
/// </summary>
public sealed record TrustDeviceCommand(
    Guid UserId,
    Guid SessionId,
    string DeviceName,
    string? StepUpReceipt,
    string? DeviceInfo = null) : ICommand<bool>;
public sealed record RevokeTrustedDeviceCommand(Guid UserId, Guid DeviceId) : ICommand<bool>;
/// <summary>
///     Names or renames one of the acting user's own trusted devices. The acting user is bound
///     by the controller from the authenticated principal, never from the request body.
/// </summary>
public sealed record RenameTrustedDeviceCommand(Guid UserId, Guid DeviceId, string Name) : ICommand<bool>;
/// <summary>
///     Names or renames one of the acting user's own active sessions. The acting user is bound
///     by the controller from the authenticated principal, never from the request body.
/// </summary>
public sealed record RenameSessionCommand(Guid UserId, Guid SessionId, string Name) : ICommand<bool>;

public sealed class SessionMutationCommandHandler(
    ISessionManagementService sessionService,
    IStepUpReceiptService stepUpReceiptService) :
    ICommandHandler<TerminateSessionCommand, bool>,
    ICommandHandler<TerminateUserSessionsCommand, int>,
    ICommandHandler<RefreshUserSessionCommand, bool>,
    ICommandHandler<TrustDeviceCommand, bool>,
    ICommandHandler<RevokeTrustedDeviceCommand, bool>,
    ICommandHandler<RenameTrustedDeviceCommand, bool>,
    ICommandHandler<RenameSessionCommand, bool>
{
    public Task<bool> Handle(TerminateSessionCommand command, CancellationToken cancellationToken) =>
        sessionService.TerminateSessionAsync(command.SessionId, command.Reason, cancellationToken);

    public Task<int> Handle(TerminateUserSessionsCommand command, CancellationToken cancellationToken) =>
        sessionService.TerminateAllUserSessionsAsync(
            command.UserId,
            command.Reason,
            command.ExceptSessionId,
            cancellationToken);

    public Task<bool> Handle(RefreshUserSessionCommand command, CancellationToken cancellationToken) =>
        sessionService.RefreshSessionAsync(command.SessionId, cancellationToken);

    public async Task<bool> Handle(TrustDeviceCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId == Guid.Empty || command.SessionId == Guid.Empty)
        {
            return false;
        }

        // Resolve the signed session inside the command transaction. Transport metadata
        // and client-selected fingerprints cannot identify the device being trusted.
        var session = await sessionService.GetSessionAsync(command.SessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || session.Id != command.SessionId || session.UserId != command.UserId ||
            !session.IsValid || session.TerminatedAt.HasValue || string.IsNullOrWhiteSpace(session.DeviceFingerprint))
        {
            return false;
        }

        // Fresh verification gate (issue #1144): a session token alone — possibly days old —
        // must not be enough to grant device trust. A step-up receipt proves the user just
        // re-presented an MFA factor for exactly this operation; the consume call enforces the
        // receipt's subject, binding, validity window, and single use.
        // Issue #1191: a missing/invalid receipt surfaces as a StepUpRequiredException so the
        // controller answers with the structured step-up challenge instructions (423) instead
        // of an ambiguous failure.
        var binding = TrustDeviceStepUpBinding(command.DeviceName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(command.StepUpReceipt))
        {
            throw new StepUpRequiredException(
                "Trusting a device requires fresh verification. Create and verify a step-up challenge for operation 'trust-device' and pass the receipt as stepUpReceipt.",
                binding.OperationType,
                binding.TargetReference,
                binding.PayloadHash);
        }

        try
        {
            await stepUpReceiptService.ConsumeAsync(
                binding,
                command.StepUpReceipt,
                cancellationToken).ConfigureAwait(false);
        }
        catch (StepUpReceiptInvalidException exception)
        {
            throw new StepUpRequiredException(
                exception.Message,
                binding.OperationType,
                binding.TargetReference,
                binding.PayloadHash,
                exception);
        }

        return await sessionService.TrustDeviceAsync(
            command.UserId,
            session.DeviceFingerprint,
            command.DeviceName ?? string.Empty,
            // The trusted-device row inherits the metadata captured by the session being
            // trusted, so the list can show browser/OS details alongside the fingerprint.
            session.DeviceInfo,
            // The trusting session's IP seeds the device's associated-IP history (issue #1144).
            string.IsNullOrWhiteSpace(session.IpAddress) ? null : session.IpAddress,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     The step-up operation binding for device trust: operation <c>trust-device</c> on
    ///     <c>current-session</c>, bound to the trimmed device name the client asked to trust.
    ///     The endpoint derives this binding when it issues a challenge; manually created
    ///     challenges must use the identical binding.
    /// </summary>
    public static StepUpOperationBinding TrustDeviceStepUpBinding(string deviceName) => new(
        "trust-device",
        "current-session",
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deviceName.Trim()))).ToLowerInvariant());


    public Task<bool> Handle(RevokeTrustedDeviceCommand command, CancellationToken cancellationToken) =>
        sessionService.RevokeTrustedDeviceAsync(command.UserId, command.DeviceId, cancellationToken);

    public Task<bool> Handle(RenameTrustedDeviceCommand command, CancellationToken cancellationToken) =>
        sessionService.RenameTrustedDeviceAsync(command.UserId, command.DeviceId, command.Name, cancellationToken);

    public Task<bool> Handle(RenameSessionCommand command, CancellationToken cancellationToken) =>
        sessionService.RenameSessionAsync(command.UserId, command.SessionId, command.Name, cancellationToken);
}
