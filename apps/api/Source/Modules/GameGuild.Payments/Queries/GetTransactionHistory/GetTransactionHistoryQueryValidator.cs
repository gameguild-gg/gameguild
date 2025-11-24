using FluentValidation;

namespace GameGuild.Payments.Queries;

public class GetTransactionHistoryQueryValidator : AbstractValidator<GetTransactionHistoryQuery>
{
    public GetTransactionHistoryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).WithMessage("Skip value cannot be negative");

        RuleFor(x => x.Take).GreaterThan(0).WithMessage("Take value must be greater than zero").LessThanOrEqualTo(1000).WithMessage("Take value cannot exceed 1000 records");

        RuleFor(x => x.TypeFilter).IsInEnum().When(x => x.TypeFilter.HasValue).WithMessage("Invalid transaction type filter");

        RuleFor(x => x.StatusFilter).IsInEnum().When(x => x.StatusFilter.HasValue).WithMessage("Invalid transaction status filter");
    }
}
