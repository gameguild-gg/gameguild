using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for DeleteOrderCommand
/// </summary>
public sealed class DeleteOrderCommandValidator : AbstractValidator<DeleteOrderCommand>
{
    public DeleteOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
    }
}
