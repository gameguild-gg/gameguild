using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for SoftDeleteTenantCommand </summary>
public class SoftDeleteTenantCommandValidator : AbstractValidator<SoftDeleteTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public SoftDeleteTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Tenant ID is required")
            .MustAsync(TenantExists)
            .WithMessage("Tenant not found")
            .MustAsync(TenantIsNotDeleted)
            .WithMessage("Tenant is already soft deleted")
            .MustAsync(TenantIsNotDefault)
            .WithMessage("Default tenant cannot be deleted");
    }

    private Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken) { return _context.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Id == tenantId, cancellationToken); }

    private async Task<bool> TenantIsNotDeleted(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return tenant is { DeletedAt: null };
    }

    private async Task<bool> TenantIsNotDefault(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return tenant is { IsDefault: false };
    }
}
