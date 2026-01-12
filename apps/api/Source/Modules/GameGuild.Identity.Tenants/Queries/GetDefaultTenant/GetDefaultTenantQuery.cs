using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get the default tenant (the tenant marked as IsDefault = true)
/// </summary>
public record GetDefaultTenantQuery : IQuery<Tenant?>;
