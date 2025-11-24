using FluentValidation;
using GameGuild.Authentication.Commands;

namespace GameGuild.Authentication.Validators;

/// <summary>
///     Validator for RevokeTokenCommand
/// </summary>
public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator() { RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required").NotNull().WithMessage("Refresh token cannot be null"); }
}
