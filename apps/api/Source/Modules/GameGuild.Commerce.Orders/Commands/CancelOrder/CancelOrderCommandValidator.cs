using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for CancelOrderCommand
/// </summary>
public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}
