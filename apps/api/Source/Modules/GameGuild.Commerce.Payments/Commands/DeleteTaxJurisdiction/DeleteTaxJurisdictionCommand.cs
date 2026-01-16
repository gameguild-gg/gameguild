using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to delete a tax jurisdiction
/// </summary>
public record DeleteTaxJurisdictionCommand(Guid JurisdictionId) : ICommand;
