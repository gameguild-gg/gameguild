using FluentValidation;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Validator for UnlockWalletCommand
/// </summary>
public sealed class UnlockWalletCommandValidator : AbstractValidator<UnlockWalletCommand>
{
    public UnlockWalletCommandValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required"); }
}
