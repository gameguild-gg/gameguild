using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for RestoreTenantCommand
/// </summary>
public class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand>
{
  public RestoreTenantCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}