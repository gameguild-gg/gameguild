using FluentValidation;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Validator for RestoreTenantCommand
/// </summary>
public class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand>
{
    public RestoreTenantCommandValidator() { RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required"); }
}
