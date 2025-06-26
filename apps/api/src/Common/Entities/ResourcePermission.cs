using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Common.Entities;

/// <summary>
/// Base class for resource-specific permissions (Layer 3 of DAC permission system)
/// Generic implementation allows strong typing for each content type
/// </summary>
public abstract class ResourcePermission<T> : WithPermissions where T : BaseEntity
{
    private Guid _resourceId;

    private T _resource = null!;

    /// <summary>
    /// Resource reference - strongly typed to the content entity
    /// </summary>
    [GraphQLType(typeof(NonNullType<UuidType>))]
    [GraphQLDescription("The ID of the resource this permission applies to")]
    [Required]
    public Guid ResourceId
    {
        get => _resourceId;
        set => _resourceId = value;
    }

    /// <summary>
    /// Navigation property to the resource entity
    /// </summary>
    [GraphQLIgnore] // Cannot use generic type parameters in GraphQL attributes
    [GraphQLDescription("The resource this permission applies to")]
    [ForeignKey(nameof(ResourceId))]
    public virtual T Resource
    {
        get => _resource;
        set => _resource = value;
    }

    // Computed properties specific to resource permissions

    /// <summary>
    /// Check if this is a default permission for this resource in a specific tenant
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Whether this is a default permission for this resource in a specific tenant")]
    public bool IsDefaultResourcePermission
    {
        get => UserId == null && TenantId != null;
    }

    /// <summary>
    /// Check if this is a global default permission for this resource
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Whether this is a global default permission for this resource")]
    public bool IsGlobalResourceDefault
    {
        get => UserId == null && TenantId == null;
    }

    /// <summary>
    /// Check if this is a user-specific permission for this resource
    /// </summary>
    [GraphQLType(typeof(NonNullType<BooleanType>))]
    [GraphQLDescription("Whether this is a user-specific permission for this resource")]
    public bool IsUserResourcePermission
    {
        get => UserId != null;
    }
}
