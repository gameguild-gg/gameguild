using FluentValidation;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Validator for RevokeTokenCommand
/// </summary>
public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator() { RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required").NotNull().WithMessage("Refresh token cannot be null"); }
}
