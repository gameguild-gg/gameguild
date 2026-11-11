using FluentValidation;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Validator for InitiateOrchestratedAuthenticationCommand following CQRS and DRY principles
/// </summary>
public sealed class InitiateOrchestratedAuthenticationCommandValidator : AbstractValidator<InitiateOrchestratedAuthenticationCommand>
{
    public InitiateOrchestratedAuthenticationCommandValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("Identifier is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");

        RuleFor(x => x.TenantId).Must(tenantId => !tenantId.HasValue || tenantId.Value != Guid.Empty).WithMessage("Tenant ID must be a valid GUID when provided");
    }
}

/// <summary>
///     Validator for ProcessOrchestratedAuthenticationStepCommand following CQRS and DRY principles
/// </summary>
public sealed class ProcessOrchestratedAuthenticationStepCommandValidator : AbstractValidator<ProcessOrchestratedAuthenticationStepCommand>
{
    public ProcessOrchestratedAuthenticationStepCommandValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty()
            .WithMessage("Flow ID is required");

        RuleFor(x => x.Step)
            .IsInEnum()
            .NotEqual(AuthenticationStep.PrimaryCredential)
            .WithMessage("The primary credential step is satisfied by flow initiation and cannot be processed.");

        RuleFor(x => x)
            .Must(command => new[] { command.TotpCode, command.BackupCode, command.WebAuthnAssertion, command.RiskCode }
                .Count(proof => !string.IsNullOrWhiteSpace(proof)) == 1)
            .WithMessage("Exactly one verification proof is required.");
        RuleFor(x => x)
            .Must(command => command.Step == AuthenticationStep.RiskChallenge
                ? !string.IsNullOrWhiteSpace(command.RiskCode)
                : string.IsNullOrWhiteSpace(command.RiskCode))
            .WithMessage("The risk challenge requires its out-of-band risk code.");
        RuleFor(x => x.RiskCode).Matches("^[0-9]{8}$").When(x => !string.IsNullOrWhiteSpace(x.RiskCode));
    }
}

/// <summary>
///     Validator for CompleteOrchestratedAuthenticationCommand following CQRS and DRY principles
/// </summary>
public sealed class CompleteOrchestratedAuthenticationCommandValidator : AbstractValidator<CompleteOrchestratedAuthenticationCommand>
{
    public CompleteOrchestratedAuthenticationCommandValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty()
            .WithMessage("Flow ID is required");
    }
}

/// <summary>
///     Validator for AbandonOrchestratedAuthenticationCommand following CQRS and DRY principles
/// </summary>
public sealed class AbandonOrchestratedAuthenticationCommandValidator : AbstractValidator<AbandonOrchestratedAuthenticationCommand>
{
    public AbandonOrchestratedAuthenticationCommandValidator()
    {
        RuleFor(x => x.FlowId)
            .NotEmpty()
            .WithMessage("Flow ID is required");
    }
}
