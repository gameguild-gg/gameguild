using FluentValidation;

namespace GameGuild.Modules.Auth;

/// <summary>
/// Validator for RevokeTokenCommand following CQRS and DRY principles
/// </summary>
public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .NotNull().WithMessage("Refresh token cannot be null")
            .MinimumLength(10).WithMessage("Refresh token appears to be invalid");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45).WithMessage("IP address is too long") // IPv6 can be up to 45 characters
            .When(x => !string.IsNullOrEmpty(x.IpAddress));
    }
}
