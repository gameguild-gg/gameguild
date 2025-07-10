using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for BulkRestoreTenantsCommand
/// </summary>
public class BulkRestoreTenantsCommandValidator : AbstractValidator<BulkRestoreTenantsCommand>
{
  public BulkRestoreTenantsCommandValidator()
  {
    RuleFor(x => x.TenantIds)
      .NotNull()
      .WithMessage("Tenant IDs collection cannot be null")
      .NotEmpty()
      .WithMessage("At least one tenant ID is required")
      .Must(ids => ids.All(id => id != Guid.Empty))
      .WithMessage("All tenant IDs must be valid (non-empty GUIDs)")
      .Must(ids => ids.Count() <= 100)
      .WithMessage("Cannot restore more than 100 tenants at once");
  }
}