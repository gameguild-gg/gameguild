using FluentValidation;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for UpdateTenantCommand
/// </summary>
public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("Tenant ID is required");

        RuleFor(x => x.Name).MaximumLength(200).WithMessage("Tenant name cannot exceed 200 characters").When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Tenant description cannot exceed 1000 characters").When(x => !string.IsNullOrEmpty(x.Description));

        // At least one field must be provided for update
        RuleFor(x => x).Must(x => !string.IsNullOrEmpty(x.Name) || !string.IsNullOrEmpty(x.Description)).WithMessage("At least one field (Name or Description) must be provided for update");
    }
}
