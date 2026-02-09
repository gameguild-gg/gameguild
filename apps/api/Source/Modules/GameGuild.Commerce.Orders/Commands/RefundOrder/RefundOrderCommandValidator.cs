using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for RefundOrderCommand
/// </summary>
public sealed class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue)
            .WithMessage("Refund amount must be positive.");
    }
}
