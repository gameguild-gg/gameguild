using FluentValidation;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Validator for LocalSignUpCommand following CQRS and DRY principles
/// </summary>
public class LocalSignUpCommandValidator : AbstractValidator<LocalSignUpCommand>
{
    public LocalSignUpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .NotNull().WithMessage("Email cannot be null")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(254).WithMessage("Email is too long"); // RFC 5321 limit

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .NotNull().WithMessage("Password cannot be null")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .MaximumLength(128).WithMessage("Password is too long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .NotNull().WithMessage("Username cannot be null")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long")
            .MaximumLength(50).WithMessage("Username is too long")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Username can only contain letters, numbers, dots, hyphens, and underscores");

        RuleFor(x => x.TenantId)
            .Must(tenantId => !tenantId.HasValue || tenantId.Value != Guid.Empty)
            .WithMessage("Tenant ID must be a valid GUID when provided");
    }
}
