using FluentValidation;


namespace GameGuild.Modules.Tenants;

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