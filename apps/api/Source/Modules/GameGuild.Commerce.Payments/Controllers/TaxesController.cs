using Asp.Versioning;
using GameGuild.CQRS;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation controller.
///     Provides endpoints for calculating taxes and validating exemptions.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/taxes")]
[Tags("taxes")]
[Authorize]
public sealed class TaxesController(ISender sender) : BaseApiController
{
    /// <summary>
    ///     Calculate tax for a transaction
    /// </summary>
    [HttpPost(":calculate")]
    [ProducesResponseType(typeof(TaxCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateTax([FromBody] CalculateTaxRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CalculateTaxCommand(
            request.JurisdictionCode,
            request.Amount,
            request.Currency,
            request.CustomerType,
            request.ProductCategory,
            request.CustomerVatNumber,
            request.IsTaxInclusive,
            request.TransactionDate,
            request.ApplicableExemptions
        );

        var result = await sender.Send(command, ct).ConfigureAwait(false);

        return Ok(result);
    }

    /// <summary>
    ///     Validate a tax exemption
    /// </summary>
    /// <param name="request">Exemption validation request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost(":validate-exemption")]
    [EndpointSummary("Validate tax exemption")]
    [EndpointDescription("Validates whether a tax exemption certificate or status is valid for a given transaction.")]
    [ProducesResponseType(typeof(TaxExemptionValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateExemption([FromBody] ValidateTaxExemptionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sender.Send(new ValidateTaxExemptionCommand(
            request.JurisdictionCode,
            request.ExemptionType,
            request.ExemptionCertificateNumber,
            request.CustomerVatNumber,
            request.CustomerId,
            request.TransactionDate), ct);

        return Ok(result);
    }
}

/// <summary>Request to validate tax exemption</summary>
public sealed record ValidateTaxExemptionRequest(
    string JurisdictionCode,
    string ExemptionType,
    string? ExemptionCertificateNumber,
    string? CustomerVatNumber,
    Guid? CustomerId,
    DateTime? TransactionDate);

/// <summary>Result of tax exemption validation</summary>
public sealed record TaxExemptionValidationResult(
    bool IsValid,
    string? ExemptionType,
    decimal ExemptionRate,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    string? ValidationMessage,
    List<string>? Warnings);

