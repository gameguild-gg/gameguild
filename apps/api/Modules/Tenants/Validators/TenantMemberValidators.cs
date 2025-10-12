using FluentValidation;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Validator for AddTenantMemberCommand
/// </summary>
public sealed class AddTenantMemberValidator : AbstractValidator<AddTenantMemberCommand>
{
    public AddTenantMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is required")
            .MaximumLength(100)
            .WithMessage("Role must not exceed 100 characters");
    }
}

/// <summary>
///     Validator for RemoveTenantMemberCommand
/// </summary>
public sealed class RemoveTenantMemberValidator : AbstractValidator<RemoveTenantMemberCommand>
{
    public RemoveTenantMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.LeaveReason)
            .MaximumLength(500)
            .WithMessage("Leave reason must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.LeaveReason));
    }
}

/// <summary>
///     Validator for UpdateTenantMemberRoleCommand
/// </summary>
public sealed class UpdateTenantMemberRoleValidator : AbstractValidator<UpdateTenantMemberRoleCommand>
{
    public UpdateTenantMemberRoleValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.NewRole)
            .NotEmpty()
            .WithMessage("New role is required")
            .MaximumLength(100)
            .WithMessage("Role must not exceed 100 characters");
    }
}

/// <summary>
///     Validator for ActivateTenantMemberCommand
/// </summary>
public sealed class ActivateTenantMemberValidator : AbstractValidator<ActivateTenantMemberCommand>
{
    public ActivateTenantMemberValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");
    }
}
