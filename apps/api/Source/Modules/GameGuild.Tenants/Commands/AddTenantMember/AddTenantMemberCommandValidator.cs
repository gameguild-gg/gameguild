using FluentValidation;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Validator for AddTenantMemberCommand
/// </summary>
public class AddTenantMemberCommandValidator : AbstractValidator<AddTenantMemberCommand>
{
    public AddTenantMemberCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Role).NotEmpty().WithMessage("Role is required").MaximumLength(50).WithMessage("Role must not exceed 50 characters");
    }
}
