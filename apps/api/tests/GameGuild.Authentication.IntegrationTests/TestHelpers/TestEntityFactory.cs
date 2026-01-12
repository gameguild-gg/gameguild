using System.Reflection;
using GameGuild.Authentication;
using GameGuild.Authorization;

namespace GameGuild.Tests.Authentication.Integration.TestHelpers;

/// <summary>
/// Factory class for creating test entities with protected properties
/// Uses reflection to set properties that are protected in EntityBase
/// </summary>
public static class TestEntityFactory
{
    /// <summary>
    /// Creates an AbacPolicy with the specified TenantId
    /// </summary>
    public static AbacPolicy CreateAbacPolicy(
        string name,
        Guid? tenantId,
        string? resourceType = null,
        AbacPolicyEffect effect = AbacPolicyEffect.Allow,
        string? attributeExpression = null,
        string? conditionExpression = null)
    {
        var policy = new AbacPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            ResourceType = resourceType,
            Effect = effect,
            AttributeExpression = attributeExpression ?? "{}",
            ConditionExpression = conditionExpression,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        SetTenantId(policy, tenantId);
        return policy;
    }

    /// <summary>
    /// Creates a ConditionalPolicy with the specified TenantId
    /// </summary>
    public static ConditionalPolicy CreateConditionalPolicy(
        string name,
        Guid? tenantId,
        PolicyConditionType conditionType = PolicyConditionType.Time,
        PolicyAction action = PolicyAction.Allow,
        string? timeConditions = null,
        string? environmentConditions = null,
        string? description = null,
        Guid? createdBy = null)
    {
        var policy = new ConditionalPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ConditionType = conditionType,
            Action = action,
            TimeConditions = timeConditions,
            EnvironmentConditions = environmentConditions,
            Priority = 0,
            IsEnabled = true,
            CreatedBy = createdBy ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        SetTenantId(policy, tenantId);
        return policy;
    }

    /// <summary>
    /// Creates a TenantPermission with the specified TenantId and Permission
    /// </summary>
    public static TenantPermission CreateTenantPermission(
        Guid userId,
        Guid tenantId,
        PermissionType permission)
    {
        var tenantPermission = new TenantPermission(userId, tenantId)
        {
            Id = Guid.NewGuid(),
            Permissions = ((int)permission).ToString(),
            GrantedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return tenantPermission;
    }

    /// <summary>
    /// Creates a ContentTypePermission with the specified TenantId
    /// </summary>
    public static ContentTypePermission CreateContentTypePermission(
        Guid userId,
        Guid tenantId,
        string contentTypeName,
        PermissionType permission)
    {
        var contentPermission = new ContentTypePermission(userId, tenantId, contentTypeName)
        {
            Id = Guid.NewGuid(),
            Permissions = ((int)permission).ToString(),
            GrantedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return contentPermission;
    }

    /// <summary>
    /// Creates a Role with the specified TenantId
    /// </summary>
    public static Role CreateRole(
        string name,
        Guid? tenantId,
        string? description = null,
        List<string>? permissions = null)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description ?? $"{name} description",
            Permissions = System.Text.Json.JsonSerializer.Serialize(permissions ?? new List<string> { "read" }),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        SetTenantId(role, tenantId);
        return role;
    }

    /// <summary>
    /// Sets the TenantId property on an entity using reflection
    /// </summary>
    private static void SetTenantId<T>(T entity, Guid? tenantId) where T : class
    {
        var property = typeof(T).GetProperty("TenantId", 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (property?.CanWrite == true)
        {
            property.SetValue(entity, tenantId);
        }
        else
        {
            // Try to set through base class
            var baseProperty = typeof(T).BaseType?.GetProperty("TenantId",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (baseProperty?.CanWrite == true)
            {
                baseProperty.SetValue(entity, tenantId);
            }
        }
    }
}
