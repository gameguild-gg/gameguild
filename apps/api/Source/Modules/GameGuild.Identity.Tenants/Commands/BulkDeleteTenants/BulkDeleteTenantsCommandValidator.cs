using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for BulkDeleteTenantsCommand
/// </summary>
public class BulkDeleteTenantsCommandValidator : AbstractValidator<BulkDeleteTenantsCommand>
{
    public BulkDeleteTenantsCommandValidator()
    {
        RuleFor(x => x.TenantIds).NotEmpty().WithMessage("At least one tenant ID is required");

        RuleForEach(x => x.TenantIds).NotEmpty().WithMessage("Tenant ID cannot be empty");

        RuleFor(x => x.TenantIds).Must(ids => ids.Count() <= 50).WithMessage("Cannot delete more than 50 tenants at once");
    }
}
