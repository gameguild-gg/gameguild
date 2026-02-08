
namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for BulkActivateTenantsCommand
/// </summary>
public class BulkActivateTenantsCommandValidator : BulkOperationValidator<BulkActivateTenantsCommand>
{
    public BulkActivateTenantsCommandValidator() { AddCommonRules(); }

    protected override IEnumerable<Guid> GetTenantIds(BulkActivateTenantsCommand instance) { return instance.TenantIds; }
}
