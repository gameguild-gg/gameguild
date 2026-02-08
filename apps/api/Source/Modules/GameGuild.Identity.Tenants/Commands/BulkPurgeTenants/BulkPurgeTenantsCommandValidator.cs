using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for BulkPurgeTenantsCommand — uses stricter limit (50) for destructive operations
/// </summary>
public class BulkPurgeTenantsCommandValidator : AbstractValidator<BulkPurgeTenantsCommand>
{
    public BulkPurgeTenantsCommandValidator()
    {
        RuleFor(x => x.TenantIds).NotEmpty().WithMessage("At least one tenant ID is required");

        RuleForEach(x => x.TenantIds).NotEmpty().WithMessage("Tenant ID cannot be empty");

        RuleFor(x => x.TenantIds).Must(ids => ids.Count() <= 50).WithMessage("Cannot purge more than 50 tenants at once");
    }
}
