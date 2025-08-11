using FluentValidation;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Validator for DeactivateTenantCommand
/// </summary>
public class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand> {
  public DeactivateTenantCommandValidator() {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}
