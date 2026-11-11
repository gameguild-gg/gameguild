using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Builds the structured step-up-required response (issue #1191) that sensitive actions
///     return (HTTP 423) when the caller did not present a valid step-up receipt. This is the
///     platform trigger the step-up machinery was missing: instead of a bare 403/409, the client
///     learns the exact operation binding to challenge, the MFA methods available to the
///     account, and the challenge window.
/// </summary>
public interface IStepUpRequiredResponder
{
    /// <summary>
    ///     Builds the step-up-required response for an account and operation binding.
    /// </summary>
    /// <param name="userId">The account the step-up applies to</param>
    /// <param name="operationType">Operation type the client must bind its challenge to</param>
    /// <param name="targetReference">Target reference the client must bind its challenge to</param>
    /// <param name="reason">Human-readable reason shown to the client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<StepUpAuthenticationRequiredResponse> BuildAsync(
        Guid userId,
        string operationType,
        string targetReference,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Builds the step-up-required response from a thrown
    ///     <see cref="StepUpRequiredException"/> (preferred when the exception carries the
    ///     exact binding, e.g. from <c>EconomyStepUpExecutor</c>).
    /// </summary>
    Task<StepUpAuthenticationRequiredResponse> BuildAsync(
        Guid userId,
        StepUpRequiredException requirement,
        CancellationToken cancellationToken = default);
}

public sealed class StepUpRequiredResponder(
    IMfaService mfaService,
    ILogger<StepUpRequiredResponder> logger) : IStepUpRequiredResponder
{
    /// <summary>
    ///     Mirrors the step-up challenge lifetime (<see cref="StepUpReceiptService"/> challenges
    ///     expire after five minutes), so the response's expiry is the window the client has to
    ///     complete a fresh ceremony.
    /// </summary>
    private static readonly TimeSpan ChallengeWindow = TimeSpan.FromMinutes(5);

    public Task<StepUpAuthenticationRequiredResponse> BuildAsync(
        Guid userId,
        StepUpRequiredException requirement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        return BuildAsync(userId, requirement.OperationType, requirement.TargetReference, requirement.Message, cancellationToken);
    }

    public async Task<StepUpAuthenticationRequiredResponse> BuildAsync(
        Guid userId,
        string operationType,
        string targetReference,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetReference);

        // Enrolled methods only: the verify path accepts factors the account actually has.
        // Best-effort — an MFA-configuration read failure must not turn a 423 into a 500.
        var availableMethods = Array.Empty<string>();
        try
        {
            var configuration = await mfaService.GetMfaConfigurationAsync(userId, cancellationToken).ConfigureAwait(false);
            if (configuration is { IsEnabled: true } && configuration.EnabledMethods is { Length: > 0 })
            {
                availableMethods = configuration.EnabledMethods;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Could not read the MFA configuration while building a step-up-required response: {ErrorType}",
                exception.GetType().FullName);
        }

        return new StepUpAuthenticationRequiredResponse
        {
            StepUpRequired = true,
            RiskLevel = RiskLevel.High,
            Reason = reason,
            AvailableMethods = [.. availableMethods],
            // The challenge ceremony is created by the already-authenticated caller
            // (POST /auth/step-up/challenges), so no temporary token is issued here.
            StepUpToken = string.Empty,
            RiskFactors = [$"operation:{operationType}", $"target:{targetReference}"],
            ExpiresAt = SystemClock.UtcNow.Add(ChallengeWindow)
        };
    }
}
