using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for DeleteTenantCommand
/// </summary>
public sealed class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Deletion reason must not exceed 500 characters");
    }
}
