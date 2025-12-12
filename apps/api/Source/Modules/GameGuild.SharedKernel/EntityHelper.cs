using System.Reflection;

namespace GameGuild.Resources.Application;

/// <summary>
///     Helper class for common entity operations
/// </summary>
public static class EntityHelper
{
    /// <summary>
    ///     Sets the TenantId property on an entity using reflection.
    ///     This is needed because TenantId has a protected setter in the base EntityBase class.
    /// </summary>
    /// <param name="entity">The entity to set the TenantId on</param>
    /// <param name="tenantId">The tenant ID to set</param>
    /// <typeparam name="T">The entity type</typeparam>
    public static void SetTenantId<T>(T entity, Guid tenantId) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var tenantIdProperty = typeof(T).GetProperty("TenantId");

        if (tenantIdProperty != null) tenantIdProperty.SetValue(entity, new TenantId(tenantId));
    }

    /// <summary>
    ///     Sets the TenantId property on an entity using reflection.
    ///     This is needed because TenantId has a protected setter in the base EntityBase class.
    /// </summary>
    /// <param name="entity">The entity to set the TenantId on</param>
    /// <param name="tenantId">The tenant ID to set</param>
    /// <typeparam name="T">The entity type</typeparam>
    public static void SetTenantId<T>(T entity, TenantId tenantId) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        var tenantIdProperty = typeof(T).GetProperty("TenantId");

        if (tenantIdProperty != null) tenantIdProperty.SetValue(entity, tenantId);
    }
}
