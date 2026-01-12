using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for DeactivateTenantCommand
/// </summary>
public class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
