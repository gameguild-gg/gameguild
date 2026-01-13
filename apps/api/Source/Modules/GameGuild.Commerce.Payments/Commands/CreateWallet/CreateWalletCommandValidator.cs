using FluentValidation;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Validator for CreateWalletCommand
/// </summary>
public sealed class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Currency).NotEmpty().WithMessage("Currency is required").Length(3).WithMessage("Currency must be a valid 3-character code");
    }
}
