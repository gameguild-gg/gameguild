using FluentValidation;

namespace GameGuild.Commerce.Payments;

public sealed class GetWalletBalanceQueryValidator : AbstractValidator<GetWalletBalanceQuery>
{
    public GetWalletBalanceQueryValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required"); }
}
