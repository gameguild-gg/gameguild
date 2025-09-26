using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for DeactivateTenantCommand </summary>
public class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public DeactivateTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Tenant ID is required")
            .MustAsync(TenantExists)
            .WithMessage("Tenant not found")
            .MustAsync(TenantIsActive)
            .WithMessage("Tenant is already inactive")
            .MustAsync(TenantIsNotDefault)
            .WithMessage("Default tenant cannot be deactivated");
    }

    private Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken) { return _context.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.DeletedAt == null, cancellationToken); }

    private async Task<bool> TenantIsActive(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && t.DeletedAt == null, cancellationToken);

        return tenant is { IsActive: true };
    }

    private async Task<bool> TenantIsNotDefault(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && t.DeletedAt == null, cancellationToken);

        return tenant is { IsDefault: false };
    }
}
