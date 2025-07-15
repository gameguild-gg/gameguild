using FluentValidation;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Validator for HardDeleteTenantCommand
/// </summary>
public class HardDeleteTenantCommandValidator : AbstractValidator<HardDeleteTenantCommand> {
  public HardDeleteTenantCommandValidator() {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}
