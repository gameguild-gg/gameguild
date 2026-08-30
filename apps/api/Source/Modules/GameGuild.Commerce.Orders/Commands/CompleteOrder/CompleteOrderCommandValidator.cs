using FluentValidation;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Validator for CompleteOrderCommand
/// </summary>
public sealed class CompleteOrderCommandValidator : AbstractValidator<CompleteOrderCommand>
{
    public CompleteOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.MarketplaceSettlement)
            .Must(settlement => settlement is null ||
                Enum.IsDefined(settlement.CurrencyChoice) &&
                !string.IsNullOrWhiteSpace(settlement.IdempotencyKey))
            .WithMessage("Marketplace settlement requires a valid currency choice and idempotency key.");
        RuleFor(x => x)
            .Must(command => command.MarketplaceSettlement is null ||
                command.PaymentId is null &&
                string.IsNullOrWhiteSpace(command.PaymentProviderReference) &&
                string.IsNullOrWhiteSpace(command.PaymentMethod))
            .WithMessage("Fiat payment references cannot be combined with Economy Marketplace settlement.");
    }
}
