using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for ArchiveTenantCommand
/// </summary>
public class ArchiveTenantCommandValidator : AbstractValidator<ArchiveTenantCommand>
{
    public ArchiveTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Reason).NotEmpty().WithMessage("Archive reason is required").MaximumLength(500).WithMessage("Archive reason must not exceed 500 characters");
    }
}
