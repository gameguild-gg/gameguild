using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for DeactivateTenantCommand
/// </summary>
public class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
  public DeactivateTenantCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}