using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for ActivateTenantCommand </summary>
public class ActivateTenantCommandValidator : AbstractValidator<ActivateTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public ActivateTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Tenant ID is required").MustAsync(TenantExists).WithMessage("Tenant not found").MustAsync(TenantIsInactive).WithMessage("Tenant is already active");
    }

    private Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken) { return _context.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.DeletedAt == null, cancellationToken); }

    private async Task<bool> TenantIsInactive(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && t.DeletedAt == null, cancellationToken);

        return tenant is { IsActive: false };
    }
}
