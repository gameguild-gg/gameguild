using FluentValidation;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Validator for RefreshTokenCommand following CQRS and DRY principles
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .NotNull().WithMessage("Refresh token cannot be null")
            .MinimumLength(10).WithMessage("Refresh token appears to be invalid");

        RuleFor(x => x.TenantId)
            .Must(tenantId => !tenantId.HasValue || tenantId.Value != Guid.Empty)
            .WithMessage("Tenant ID must be a valid GUID when provided");
    }
}
