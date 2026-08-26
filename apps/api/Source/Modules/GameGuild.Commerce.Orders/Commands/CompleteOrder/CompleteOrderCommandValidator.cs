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
                !string.IsNullOrWhiteSpace(settlement.JurisdictionCode) &&
                settlement.RiskDecisionId != Guid.Empty &&
                !string.IsNullOrWhiteSpace(settlement.OperationFingerprint) &&
                !string.IsNullOrWhiteSpace(settlement.IdempotencyKey))
            .WithMessage("Marketplace settlement requires valid currency, jurisdiction, risk, fingerprint and idempotency evidence.");
        RuleFor(x => x)
            .Must(command => command.MarketplaceSettlement is null ||
                command.PaymentId is null &&
                string.IsNullOrWhiteSpace(command.PaymentProviderReference) &&
                string.IsNullOrWhiteSpace(command.PaymentMethod))
            .WithMessage("Fiat payment references cannot be combined with Economy Marketplace settlement.");
    }
}
