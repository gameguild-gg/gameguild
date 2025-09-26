using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for UpdateTenantCommand </summary>
public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public UpdateTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Tenant ID is required").MustAsync(TenantExists).WithMessage("Tenant not found");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tenant name is required")
            .MaximumLength(100)
            .WithMessage("Tenant name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
            .WithMessage("Tenant name can only contain letters, numbers, spaces, hyphens, underscores, and periods")
            .When(x => x.Name is not null);

        RuleFor(x => x).MustAsync(HaveUniqueName).WithMessage("Tenant name already exists").When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description cannot exceed 500 characters").When(x => x.Description is not null);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Tenant slug is required")
            .MaximumLength(255)
            .WithMessage("Tenant slug cannot exceed 255 characters")
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Tenant slug can only contain lowercase letters, numbers, and hyphens")
            .When(x => x.Slug is not null);

        RuleFor(x => x).MustAsync(HaveUniqueSlug).WithMessage("Tenant slug already exists").When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x).Must(HaveAtLeastOneFieldToUpdate).WithMessage("At least one value must be provided to update");
    }

    private Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken) { return _context.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.DeletedAt == null, cancellationToken); }

    private async Task<bool> HaveUniqueName(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        return !await _context.Tenants.AnyAsync(tenant => tenant.Id != command.Id && tenant.DeletedAt == null && tenant.Name == command.Name, cancellationToken);
    }

    private async Task<bool> HaveUniqueSlug(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        return !await _context.Tenants.AnyAsync(tenant => tenant.Id != command.Id && tenant.DeletedAt == null && tenant.Slug == command.Slug, cancellationToken);
    }

    private static bool HaveAtLeastOneFieldToUpdate(UpdateTenantCommand command)
    {
        return !string.IsNullOrWhiteSpace(command.Name) || command.Description is not null || command.IsActive.HasValue || !string.IsNullOrWhiteSpace(command.Slug);
    }
}
