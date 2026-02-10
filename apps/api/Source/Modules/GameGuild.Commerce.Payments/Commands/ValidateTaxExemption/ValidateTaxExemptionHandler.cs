using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for ValidateTaxExemptionCommand
/// </summary>
public sealed class ValidateTaxExemptionHandler : ICommandHandler<ValidateTaxExemptionCommand, TaxExemptionValidationResult>
{
    public Task<TaxExemptionValidationResult> Handle(ValidateTaxExemptionCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would validate actual exemption
        var result = new TaxExemptionValidationResult(
            IsValid: true,
            ExemptionType: request.ExemptionType,
            ExemptionRate: 1.0m,
            ValidFrom: SystemClock.UtcNow.AddYears(-1),
            ValidTo: SystemClock.UtcNow.AddYears(1),
            ValidationMessage: "Exemption validated successfully",
            Warnings: null);

        return Task.FromResult(result);
    }
}
