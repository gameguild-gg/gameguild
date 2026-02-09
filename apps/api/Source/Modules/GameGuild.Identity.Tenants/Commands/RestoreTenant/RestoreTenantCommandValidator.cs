using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for RestoreTenantCommand
/// </summary>
public sealed class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand>
{
    public RestoreTenantCommandValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
