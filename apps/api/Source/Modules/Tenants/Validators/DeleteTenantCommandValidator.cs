using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for DeleteTenantCommand
/// </summary>
public class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
  public DeleteTenantCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}