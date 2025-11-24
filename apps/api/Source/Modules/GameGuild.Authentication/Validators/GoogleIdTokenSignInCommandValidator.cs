using FluentValidation;
using GameGuild.Authentication.Commands;

namespace GameGuild.Authentication.Validators;

/// <summary>
///     Validator for GoogleIdTokenSignInCommand following CQRS and DRY principles
/// </summary>
public class GoogleIdTokenSignInCommandValidator : AbstractValidator<GoogleIdTokenSignInCommand>
{
    public GoogleIdTokenSignInCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("ID token is required")
            .NotNull()
            .WithMessage("ID token cannot be null")
            .MaximumLength(8192)
            .WithMessage("ID token is too long"); // Google JWT tokens can be quite large

        RuleFor(x => x.TenantId).Must(tenantId => !tenantId.HasValue || tenantId.Value != Guid.Empty).WithMessage("Tenant ID must be a valid GUID when provided");
    }
}
