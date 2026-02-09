using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for RemoveTenantMemberCommand
/// </summary>
public sealed class RemoveTenantMemberCommandValidator : AbstractValidator<RemoveTenantMemberCommand>
{
    public RemoveTenantMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
    }
}
