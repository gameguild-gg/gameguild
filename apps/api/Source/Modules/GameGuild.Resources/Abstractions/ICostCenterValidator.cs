namespace GameGuild.Resources;

/// <summary>
///     Validates cost allocation cost-center codes before reports are exported or updated.
/// </summary>
public interface ICostCenterValidator
{
    Task<CostCenterValidationResult> ValidateAsync(Guid tenantId, string costCenter, CancellationToken cancellationToken = default);
}
