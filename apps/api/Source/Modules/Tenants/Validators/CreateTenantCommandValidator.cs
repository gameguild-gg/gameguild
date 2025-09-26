using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for CreateTenantCommand </summary>
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public CreateTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tenant name is required")
            .MaximumLength(100)
            .WithMessage("Tenant name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$")
            .WithMessage("Tenant name can only contain letters, numbers, spaces, hyphens, underscores, and periods")
            .MustAsync(NameIsUnique)
            .WithMessage("Tenant name already exists");

        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description cannot exceed 500 characters").When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Tenant slug is required")
            .MaximumLength(255)
            .WithMessage("Tenant slug cannot exceed 255 characters")
            .Matches(@"^[a-z0-9\-]+$")
            .WithMessage("Tenant slug can only contain lowercase letters, numbers, and hyphens")
            .MustAsync(SlugIsUnique)
            .WithMessage("Tenant slug already exists");
    }

    private async Task<bool> NameIsUnique(string name, CancellationToken cancellationToken)
    {
        bool exists = await _context.Tenants.AnyAsync(tenant => tenant.DeletedAt == null && tenant.Name == name, cancellationToken);

        return !exists;
    }

    private async Task<bool> SlugIsUnique(string slug, CancellationToken cancellationToken)
    {
        bool exists = await _context.Tenants.AnyAsync(tenant => tenant.DeletedAt == null && tenant.Slug == slug, cancellationToken);

        return !exists;
    }
}
