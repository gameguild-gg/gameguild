using FluentValidation;


namespace GameGuild.Modules.Authentication.Validators;

/// <summary>
/// Validator for LocalSignInCommand following CQRS and DRY principles
/// </summary>
public class LocalSignInCommandValidator : AbstractValidator<LocalSignInCommand>
{
    public LocalSignInCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .NotNull().WithMessage("Email cannot be null")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(254).WithMessage("Email is too long"); // RFC 5321 limit

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .NotNull().WithMessage("Password cannot be null")
            .MaximumLength(128).WithMessage("Password is too long");

        RuleFor(x => x.TenantId)
            .Must(tenantId => !tenantId.HasValue || tenantId.Value != Guid.Empty)
            .WithMessage("Tenant ID must be a valid GUID when provided");
    }
}
