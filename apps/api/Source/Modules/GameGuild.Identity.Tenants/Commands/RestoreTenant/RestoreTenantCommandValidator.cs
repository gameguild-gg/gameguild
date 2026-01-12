using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for RestoreTenantCommand
/// </summary>
public class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand>
{
    public RestoreTenantCommandValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
