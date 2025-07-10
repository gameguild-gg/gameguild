using FluentValidation;
using GameGuild.Modules.Tenants.Commands;


namespace GameGuild.Modules.Tenants.Validators;

/// <summary>
/// Validator for UpdateTenantCommand
/// </summary>
public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
  public UpdateTenantCommandValidator()
  {
    RuleFor(x => x.Id)
      .NotEmpty()
      .WithMessage("Tenant ID is required");

    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Tenant name is required")
      .MaximumLength(100)
      .WithMessage("Tenant name cannot exceed 100 characters")
      .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
      .WithMessage("Tenant name can only contain letters, numbers, spaces, hyphens, underscores, and periods");

    RuleFor(x => x.Description)
      .MaximumLength(500)
      .WithMessage("Description cannot exceed 500 characters")
      .When(x => !string.IsNullOrEmpty(x.Description));

    RuleFor(x => x.Slug)
      .NotEmpty()
      .WithMessage("Tenant slug is required")
      .MaximumLength(255)
      .WithMessage("Tenant slug cannot exceed 255 characters")
      .Matches(@"^[a-z0-9\-]+$")
      .WithMessage("Tenant slug can only contain lowercase letters, numbers, and hyphens");
  }
}