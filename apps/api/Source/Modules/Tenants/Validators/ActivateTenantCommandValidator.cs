using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for ActivateTenantCommand
/// </summary>
public class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
  public ActivateTenantCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}