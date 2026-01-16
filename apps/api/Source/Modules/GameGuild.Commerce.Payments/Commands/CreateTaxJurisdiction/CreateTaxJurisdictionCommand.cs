using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to create a tax jurisdiction
/// </summary>
public record CreateTaxJurisdictionCommand(
    string Code,
    string Name,
    string Country,
    string? State,
    string TaxType,
    decimal DefaultRate) : ICommand<Guid>;
