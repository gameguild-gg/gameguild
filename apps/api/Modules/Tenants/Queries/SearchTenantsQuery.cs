using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to search tenants by various criteria </summary>
public class SearchTenantsQuery : IQuery<Result<IEnumerable<Tenant>>>
{
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsArchived { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public string? AdminEmail { get; init; }
    public int? MaxResults { get; init; } = 100;
}