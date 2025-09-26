using FluentValidation;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Tenants;

/// <summary> Validator for RestoreTenantCommand </summary>
public class RestoreTenantCommandValidator : AbstractValidator<RestoreTenantCommand>
{
    private readonly ApplicationDbContext _context;

    public RestoreTenantCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id).NotEmpty().WithMessage("Tenant ID is required").MustAsync(TenantExists).WithMessage("Tenant not found").MustAsync(TenantIsSoftDeleted).WithMessage("Tenant is not soft deleted");
    }

    private Task<bool> TenantExists(Guid tenantId, CancellationToken cancellationToken) { return _context.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Id == tenantId, cancellationToken); }

    private async Task<bool> TenantIsSoftDeleted(Guid tenantId, CancellationToken cancellationToken)
    {
        Tenant? tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return tenant is { DeletedAt: not null };
    }
}
