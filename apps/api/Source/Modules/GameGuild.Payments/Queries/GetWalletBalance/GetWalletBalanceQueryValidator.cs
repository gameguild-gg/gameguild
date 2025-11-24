using FluentValidation;

namespace GameGuild.Payments.Queries;

public class GetWalletBalanceQueryValidator : AbstractValidator<GetWalletBalanceQuery>
{
    public GetWalletBalanceQueryValidator() { RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required"); }
}
