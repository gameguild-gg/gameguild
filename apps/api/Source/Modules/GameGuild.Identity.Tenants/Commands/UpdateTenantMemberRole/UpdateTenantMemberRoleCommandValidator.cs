using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for UpdateTenantMemberRoleCommand
/// </summary>
public class UpdateTenantMemberRoleCommandValidator : AbstractValidator<UpdateTenantMemberRoleCommand>
{
    public UpdateTenantMemberRoleCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.NewRole).NotEmpty().WithMessage("New role is required").MaximumLength(50).WithMessage("Role must not exceed 50 characters");
    }
}
