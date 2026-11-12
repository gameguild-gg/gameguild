using GameGuild.Identity.Authentication;

namespace GameGuild.API.UnitTests.Controllers;

/// <summary>
///     Deterministic <see cref="IStepUpRequiredResponder"/> for controller unit tests: returns a
///     fixed response body so the structured 423 mapping (issue #1191) can be asserted without
///     the MFA-configuration dependency of the real responder.
/// </summary>
internal sealed class StubStepUpResponder : IStepUpRequiredResponder
{
    public static readonly StubStepUpResponder Instance = new();

    public Task<StepUpAuthenticationRequiredResponse> BuildAsync(
        Guid userId,
        string operationType,
        string targetReference,
        string reason,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new StepUpAuthenticationRequiredResponse
        {
            Reason = reason,
            RiskFactors = [$"operation:{operationType}", $"target:{targetReference}"]
        });

    public Task<StepUpAuthenticationRequiredResponse> BuildAsync(
        Guid userId,
        StepUpRequiredException requirement,
        CancellationToken cancellationToken = default) =>
        BuildAsync(userId, requirement.OperationType, requirement.TargetReference, requirement.Message, cancellationToken);
}
