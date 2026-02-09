using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to partially update a tax jurisdiction
/// </summary>
public sealed record PatchTaxJurisdictionCommand(
    Guid JurisdictionId,
    string? Name,
    string? TaxType,
    decimal? DefaultRate,
    bool? IsActive) : ICommand;
