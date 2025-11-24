using FluentValidation;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Validator for GetPaymentHistoryQuery
/// </summary>
public sealed class GetPaymentHistoryQueryValidator : AbstractValidator<GetPaymentHistoryQuery>
{
    public GetPaymentHistoryQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().When(x => !x.IsAdminRequest).WithMessage("User ID is required for non-admin requests");

        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("Page number must be greater than zero");

        RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate).When(x => x.StartDate.HasValue && x.EndDate.HasValue).WithMessage("Start date must be before or equal to end date");
    }
}
