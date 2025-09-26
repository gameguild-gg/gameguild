using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for HardDeleteTenantCommand </summary>
public class HardDeleteTenantCommandValidator : AbstractValidator<HardDeleteTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public HardDeleteTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Tenant ID is required").MustAsync(TenantExists).WithMessage("Tenant not found").MustAsync(TenantIsNotDefault).WithMessage("Default tenant cannot be deleted");
    }

    private Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken) { return _context.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Id == tenantId, cancellationToken); }

    private async Task<bool> TenantIsNotDefault(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return tenant is { IsDefault: false };
    }
}
