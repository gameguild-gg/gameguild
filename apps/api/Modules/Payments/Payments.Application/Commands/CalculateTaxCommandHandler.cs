using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;


namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Handler for CalculateTaxCommand
/// </summary>
public class CalculateTaxCommandHandler : IRequestHandler<CalculateTaxCommand, TaxCalculationResult>
{
    private readonly ITaxCalculationService _taxCalculationService;
    private readonly ILogger<CalculateTaxCommandHandler> _logger;

    public CalculateTaxCommandHandler(
        ITaxCalculationService taxCalculationService,
        ILogger<CalculateTaxCommandHandler> logger)
    {
        _taxCalculationService = taxCalculationService;
        _logger = logger;
    }

    public async Task<TaxCalculationResult> Handle(CalculateTaxCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Calculating tax for {Jurisdiction}, Amount: {Amount} {Currency}",
            request.JurisdictionCode, request.Amount, request.Currency);

        var customerType = Enum.Parse<CustomerType>(request.CustomerType, ignoreCase: true);

        var taxRequest = new TaxCalculationRequest
        {
            JurisdictionCode = request.JurisdictionCode,
            Amount = request.Amount,
            Currency = request.Currency,
            CustomerType = customerType,
            ProductCategory = request.ProductCategory,
            CustomerVatNumber = request.CustomerVatNumber,
            IsTaxInclusive = request.IsTaxInclusive,
            TransactionDate = request.TransactionDate ?? DateTime.UtcNow
        };

        var result = await _taxCalculationService.CalculateTaxAsync(taxRequest, cancellationToken);

        _logger.LogInformation(
            "Tax calculated: Subtotal {Subtotal}, Tax {Tax}, Total {Total}, Rate {Rate}%",
            result.SubtotalAmount, result.TaxAmount, result.TotalAmount, result.EffectiveTaxRate);

        return result;
    }
}
