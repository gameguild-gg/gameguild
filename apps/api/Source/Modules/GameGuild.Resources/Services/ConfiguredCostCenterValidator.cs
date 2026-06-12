using Microsoft.Extensions.Options;

namespace GameGuild.Resources;

/// <summary>
///     Validates cost centers against Resources configuration until a dedicated Finance module owns the catalog.
/// </summary>
public sealed class ConfiguredCostCenterValidator(IOptions<ResourcesOptions> options) : ICostCenterValidator
{
    public Task<CostCenterValidationResult> ValidateAsync(Guid tenantId, string costCenter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(costCenter))
        {
            return Task.FromResult(CostCenterValidationResult.Invalid("Cost center is required."));
        }

        var allowed = options.Value.AllowedCostCenters;
        if (allowed.Length > 0 && !allowed.Contains(costCenter, StringComparer.OrdinalIgnoreCase))
        {
            return Task.FromResult(CostCenterValidationResult.Invalid($"Cost center '{costCenter}' is not configured for tenant {tenantId}."));
        }

        return Task.FromResult(CostCenterValidationResult.Validated());
    }
}
