using FluentValidation;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Validator for CreateTenantCommand
/// </summary>
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tenant name is required").MaximumLength(200).WithMessage("Tenant name cannot exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Tenant slug is required")
            .MaximumLength(100)
            .WithMessage("Tenant slug cannot exceed 100 characters")
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Tenant slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .WithMessage("Administrator email is required")
            .EmailAddress()
            .WithMessage("Administrator email must be a valid email address")
            .MaximumLength(320)
            .WithMessage("Administrator email cannot exceed 320 characters");

        RuleFor(x => x.Description).MaximumLength(1000).WithMessage("Tenant description cannot exceed 1000 characters").When(x => !string.IsNullOrEmpty(x.Description));
    }
}
