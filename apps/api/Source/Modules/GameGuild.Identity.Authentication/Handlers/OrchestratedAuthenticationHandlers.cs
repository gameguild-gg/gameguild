using FluentValidation;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Initiates an orchestrated authentication flow: the orchestrator validates the
///     local primary credential, assesses risk and persists the flow state with the
///     required challenge ladder. IP address and user agent are captured server-side
///     from the request context, never from the request body.
/// </summary>
public sealed class InitiateOrchestratedAuthenticationHandler(
    IAuthenticationOrchestrationService orchestrationService,
    IAuthAttemptService authAttemptService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<InitiateOrchestratedAuthenticationHandler> logger,
    IValidator<InitiateOrchestratedAuthenticationCommand> validator
) : ICommandHandler<InitiateOrchestratedAuthenticationCommand, OrchestratedFlowStateResponse>
{
    public async Task<OrchestratedFlowStateResponse> Handle(InitiateOrchestratedAuthenticationCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)));
        }

        var httpContext = httpContextAccessor.HttpContext;
        var request = new LocalAuthenticationInitiationRequest
        {
            Identifier = command.Identifier,
            CredentialType = "Email",
            AuthMethod = "Local",
            TenantId = command.TenantId,
            IpAddress = authAttemptService.GetClientIpAddress(httpContext) ?? "unknown",
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown",
            DeviceFingerprint = command.DeviceFingerprint,
            Context = new Dictionary<string, object> { ["password"] = command.Password }
        };

        var flowState = await orchestrationService.InitiateAuthenticationAsync(request, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Orchestrated authentication flow {FlowId} initiated for identifier {Identifier} with {StepCount} required steps",
            flowState.FlowId,
            // codeql[cs/exposure-of-sensitive-information] identifier is redacted by LogRedaction.MaskEmail before entering the structured log
            LogRedaction.MaskEmail(command.Identifier),
            flowState.RequiredSteps.Count);

        return AuthenticationMappings.ToDto(flowState);
    }
}

/// <summary>
///     Satisfies one challenge step of an orchestrated authentication flow by delegating
///     verification (TOTP / backup code / WebAuthn / dedicated risk code) and state transition to the orchestrator.
/// </summary>
public sealed class ProcessOrchestratedAuthenticationStepHandler(
    IAuthenticationOrchestrationService orchestrationService,
    ILogger<ProcessOrchestratedAuthenticationStepHandler> logger,
    IValidator<ProcessOrchestratedAuthenticationStepCommand> validator
) : ICommandHandler<ProcessOrchestratedAuthenticationStepCommand, OrchestratedFlowStateResponse>
{
    public async Task<OrchestratedFlowStateResponse> Handle(ProcessOrchestratedAuthenticationStepCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)));
        }

        var payload = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(command.TotpCode)) { payload["totpCode"] = command.TotpCode.Trim(); }
        if (!string.IsNullOrWhiteSpace(command.BackupCode)) { payload["backupCode"] = command.BackupCode.Trim(); }
        if (!string.IsNullOrWhiteSpace(command.WebAuthnAssertion)) { payload["webAuthnAssertion"] = command.WebAuthnAssertion.Trim(); }
        if (!string.IsNullOrWhiteSpace(command.RiskCode)) { payload["riskCode"] = command.RiskCode.Trim(); }

        var flowState = await orchestrationService
            .ProcessAuthenticationStepAsync(command.FlowId, command.Step, payload, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Orchestrated authentication flow {FlowId} step {Step} processed; complete: {IsComplete}",
            flowState.FlowId, command.Step, flowState.IsComplete);

        return AuthenticationMappings.ToDto(flowState);
    }
}

/// <summary>
///     Completes an orchestrated authentication flow whose steps are all satisfied and
///     maps the issued credentials onto the shared sign-in response.
/// </summary>
public sealed class CompleteOrchestratedAuthenticationHandler(
    IAuthenticationOrchestrationService orchestrationService,
    ILogger<CompleteOrchestratedAuthenticationHandler> logger,
    IValidator<CompleteOrchestratedAuthenticationCommand> validator
) : ICommandHandler<CompleteOrchestratedAuthenticationCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(CompleteOrchestratedAuthenticationCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)));
        }

        var result = await orchestrationService.CompleteAuthenticationAsync(command.FlowId, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Orchestrated authentication flow {FlowId} completed for user {UserId}",
            command.FlowId, result.UserId);

        return AuthenticationMappings.ToSignInResponse(result);
    }
}

/// <summary>
///     Abandons an in-flight orchestrated authentication flow; the flow then fails
///     closed for every later step/completion/lookup call.
/// </summary>
public sealed class AbandonOrchestratedAuthenticationHandler(
    IAuthenticationOrchestrationService orchestrationService,
    ILogger<AbandonOrchestratedAuthenticationHandler> logger,
    IValidator<AbandonOrchestratedAuthenticationCommand> validator
) : ICommandHandler<AbandonOrchestratedAuthenticationCommand>
{
    public async Task<GameGuild.CQRS.Unit> Handle(AbandonOrchestratedAuthenticationCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)));
        }

        await orchestrationService.AbandonAuthenticationFlowAsync(command.FlowId, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Orchestrated authentication flow {FlowId} abandoned", command.FlowId);

        return GameGuild.CQRS.Unit.Value;
    }
}

/// <summary>
///     Returns the current state of an orchestrated authentication flow;
///     abandoned, expired and unknown flows are not observable.
/// </summary>
public sealed class GetOrchestratedAuthenticationFlowStateHandler(
    IAuthenticationOrchestrationService orchestrationService
) : IQueryHandler<GetOrchestratedAuthenticationFlowStateQuery, OrchestratedFlowStateResponse?>
{
    public async Task<OrchestratedFlowStateResponse?> Handle(GetOrchestratedAuthenticationFlowStateQuery query, CancellationToken cancellationToken)
    {
        var flowState = await orchestrationService.GetFlowStateAsync(query.FlowId, cancellationToken).ConfigureAwait(false);
        return flowState is null ? null : AuthenticationMappings.ToDto(flowState);
    }
}
