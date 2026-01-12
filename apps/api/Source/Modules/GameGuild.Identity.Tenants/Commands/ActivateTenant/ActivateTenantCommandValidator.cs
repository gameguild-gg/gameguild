using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for ActivateTenantCommand
/// </summary>
public class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    public ActivateTenantCommandValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
