using GameGuild.Entities;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Generic resource permission implementation
///     Can be used for any resource type that inherits from EntityBase
/// </summary>
public class GenericResourcePermission : ResourcePermission<EntityBase>
{
    /// <summary>
    ///     Default constructor for Entity Framework
    /// </summary>
    public GenericResourcePermission() { }

    /// <summary>
    ///     Constructor for creating a generic resource permission
    /// </summary>
    /// <param name="userId">User ID who receives the permission</param>
    /// <param name="tenantId">Tenant ID (null for global permissions)</param>
    /// <param name="resourceId">ID of the specific resource</param>
    /// <param name="resourceTypeName">Name of the resource type</param>
    public GenericResourcePermission(Guid userId, Guid? tenantId, Guid resourceId, string resourceTypeName) : base(userId, tenantId, resourceId)
    {
        ResourceType = resourceTypeName ?? throw new ArgumentNullException(nameof(resourceTypeName));
    }
}
