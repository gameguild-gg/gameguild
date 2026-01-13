using FluentValidation;

namespace GameGuild.Commerce.Payments;

public class GetWalletBalanceQueryValidator : AbstractValidator<GetWalletBalanceQuery>
{
    public GetWalletBalanceQueryValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required"); }
}
