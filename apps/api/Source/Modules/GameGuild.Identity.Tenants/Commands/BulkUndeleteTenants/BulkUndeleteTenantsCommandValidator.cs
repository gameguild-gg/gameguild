namespace GameGuild.Identity.Tenants;

/// <summary>
///     Validator for BulkUndeleteTenantsCommand
/// </summary>
public class BulkUndeleteTenantsCommandValidator : BulkOperationValidator<BulkUndeleteTenantsCommand>
{
    public BulkUndeleteTenantsCommandValidator() { AddCommonRules(); }

    protected override IEnumerable<Guid> GetTenantIds(BulkUndeleteTenantsCommand instance) { return instance.TenantIds; }
}
