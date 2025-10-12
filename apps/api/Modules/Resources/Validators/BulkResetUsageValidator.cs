using FluentValidation;
using GameGuild.Modules.Resources.Commands;

namespace GameGuild.Modules.Resources.Validators;

/// <summary>
/// Validator for BulkResetUsageCommand
/// </summary>
public class BulkResetUsageValidator : AbstractValidator<BulkResetUsageCommand>
{
    public BulkResetUsageValidator()
    {
        RuleFor(x => x.TenantIds)
            .NotNull()
            .WithMessage("Tenant IDs list cannot be null");

        RuleFor(x => x.TenantIds)
            .NotEmpty()
            .WithMessage("At least one tenant ID must be provided");

        RuleFor(x => x.TenantIds)
            .Must(ids => ids.Count <= 1000)
            .WithMessage("Cannot reset more than 1000 tenants in a single operation");

        RuleFor(x => x.TenantIds)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All tenant IDs must be valid GUIDs");

        RuleFor(x => x.TenantIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate tenant IDs are not allowed");
    }
}
