using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for UpdateOrderCommand
/// </summary>
public sealed class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency != null)
            .WithMessage("Currency must be a 3-letter ISO 4217 code.");
    }
}
