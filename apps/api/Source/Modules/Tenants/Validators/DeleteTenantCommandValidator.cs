using FluentValidation;


namespace GameGuild.Modules.Tenants;

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