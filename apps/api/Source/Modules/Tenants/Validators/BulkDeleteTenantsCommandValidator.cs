using FluentValidation;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Validator for BulkDeleteTenantsCommand
/// </summary>
public class BulkDeleteTenantsCommandValidator : AbstractValidator<BulkDeleteTenantsCommand>
{
  public BulkDeleteTenantsCommandValidator()
  {
    RuleFor(x => x.TenantIds)
      .NotNull()
      .WithMessage("Tenant IDs collection cannot be null")
      .NotEmpty()
      .WithMessage("At least one tenant ID is required")
      .Must(ids => ids.All(id => id != Guid.Empty))
      .WithMessage("All tenant IDs must be valid (non-empty GUIDs)")
      .Must(ids => ids.Count() <= 100)
      .WithMessage("Cannot delete more than 100 tenants at once");
  }
}