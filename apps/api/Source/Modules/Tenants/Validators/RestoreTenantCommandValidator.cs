using FluentValidation;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Validator for RestoreTenantCommand
/// </summary>
public class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand> {
  public RestoreTenantCommandValidator() {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");
  }
}
