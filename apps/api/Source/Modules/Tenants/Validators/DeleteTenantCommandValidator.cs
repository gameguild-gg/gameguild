using FluentValidation;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Validator for DeleteTenantCommand
/// </summary>
public class SoftDeleteTenantCommandValidator : AbstractValidator<SoftDeleteTenantCommand>
{
    public SoftDeleteTenantCommandValidator() { RuleFor(x => x.Id).NotEmpty().WithMessage("Tenant ID is required"); }
}
