using FluentValidation;
using GameGuild.Modules.Permissions.Commands;

namespace GameGuild.Modules.Permissions.Validators;

/// <summary>
/// Validator for GrantTenantPermissionCommand
/// </summary>
public class GrantTenantPermissionCommandValidator : AbstractValidator<GrantTenantPermissionCommand>
{
    public GrantTenantPermissionCommandValidator()
    {
        RuleFor(x => x.Permissions)
            .NotEmpty()
            .WithMessage("At least one permission must be specified");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .When(x => x.UserId.HasValue)
            .WithMessage("User ID must be a valid GUID");

        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty)
            .When(x => x.TenantId.HasValue)
            .WithMessage("Tenant ID must be a valid GUID");
    }
}