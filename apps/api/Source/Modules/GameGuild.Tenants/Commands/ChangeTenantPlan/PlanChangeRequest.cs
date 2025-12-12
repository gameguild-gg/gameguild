namespace GameGuild.Tenants.Commands;

/// <summary>
///     Request model for plan changes (upgrade/downgrade)
/// </summary>
/// <param name="NewPlanId">New plan identifier</param>
public record PlanChangeRequest(Guid NewPlanId);
