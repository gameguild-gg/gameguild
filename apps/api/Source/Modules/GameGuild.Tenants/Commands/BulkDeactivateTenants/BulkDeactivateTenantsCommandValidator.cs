using FluentValidation;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Validator for BulkDeactivateTenantsCommand
/// </summary>
public class BulkDeactivateTenantsCommandValidator : AbstractValidator<BulkDeactivateTenantsCommand>
{
    public BulkDeactivateTenantsCommandValidator()
    {
        RuleFor(x => x.TenantIds).NotEmpty().WithMessage("At least one tenant ID is required");

        RuleForEach(x => x.TenantIds).NotEmpty().WithMessage("Tenant ID cannot be empty");

        RuleFor(x => x.TenantIds).Must(ids => ids.Count() <= 100).WithMessage("Cannot process more than 100 tenants at once");
    }
}
