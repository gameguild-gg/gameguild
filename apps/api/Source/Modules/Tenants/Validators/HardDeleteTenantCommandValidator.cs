using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for HardDeleteTenantCommand
/// </summary>
public class HardDeleteTenantCommandValidator : AbstractValidator<HardDeleteTenantCommand>
{
  public HardDeleteTenantCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}