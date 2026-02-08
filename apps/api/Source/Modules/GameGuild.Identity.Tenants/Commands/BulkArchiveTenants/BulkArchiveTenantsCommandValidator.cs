
namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for BulkArchiveTenantsCommand
/// </summary>
public class BulkArchiveTenantsCommandValidator : BulkOperationValidator<BulkArchiveTenantsCommand>
{
    public BulkArchiveTenantsCommandValidator() { AddCommonRules(); }

    protected override IEnumerable<Guid> GetTenantIds(BulkArchiveTenantsCommand instance) { return instance.TenantIds; }
}
