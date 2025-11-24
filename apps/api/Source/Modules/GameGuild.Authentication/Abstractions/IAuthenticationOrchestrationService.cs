using GameGuild.Authentication.Enums;
using GameGuild.Authentication.Models.Flow;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Orchestration service that coordinates complex authentication flows.
///     Handles multi-step authentication, conditional challenges, and state management.
/// </summary>
public interface IAuthenticationOrchestrationService
{
    /// <summary>
    ///     Initiates an authentication flow and determines required steps.
    /// </summary>
    /// <param name="request">Initial authentication request</param>
    /// <returns>Authentication flow state with required steps</returns>
    Task<AuthenticationFlowState> InitiateAuthenticationAsync(InitiateAuthenticationRequest request);

    /// <summary>
    ///     Processes a step in the authentication flow.
    /// </summary>
    /// <param name="flowId">The authentication flow ID</param>
    /// <param name="step">The step being processed</param>
    /// <param name="stepData">Data for the current step</param>
    /// <returns>Updated flow state</returns>
    Task<AuthenticationFlowState> ProcessAuthenticationStepAsync(Guid flowId, AuthenticationStep step, object stepData);

    /// <summary>
    ///     Completes an authentication flow and issues tokens.
    /// </summary>
    /// <param name="flowId">The authentication flow ID</param>
    /// <returns>Authentication result with tokens</returns>
    Task<AuthenticationResult> CompleteAuthenticationAsync(Guid flowId);

    /// <summary>
    ///     Abandons an incomplete authentication flow.
    /// </summary>
    /// <param name="flowId">The authentication flow ID</param>
    Task AbandonAuthenticationFlowAsync(Guid flowId);

    /// <summary>
    ///     Gets the current state of an authentication flow.
    /// </summary>
    /// <param name="flowId">The authentication flow ID</param>
    /// <returns>Current flow state</returns>
    Task<AuthenticationFlowState?> GetFlowStateAsync(Guid flowId);

    /// <summary>
    ///     Determines if additional authentication challenges are required based on risk assessment.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="riskScore">Current risk score</param>
    /// <param name="context">Authentication context</param>
    /// <returns>List of required additional challenges</returns>
    Task<List<AuthenticationStep>> DetermineRequiredChallengesAsync(Guid userId, double riskScore, AuthenticationAttemptContext context);
}
