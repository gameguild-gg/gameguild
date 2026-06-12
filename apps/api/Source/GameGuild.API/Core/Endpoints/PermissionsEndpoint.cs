using System.Security.Cryptography;
using System.Text;
using GameGuild.Identity.Authorization;

using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Endpoints for permission management
/// </summary>
public static class PermissionsEndpoint
{
    /// <summary>
    ///     Maps permission management endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapPermissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var permissionsGroup = app.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();

        permissionsGroup.MapGet("/", GetAllPermissions).WithName("GetAllPermissions").WithOpenApi();

        permissionsGroup.MapGet("/{id:guid}", GetPermissionById).WithName("GetPermissionById").WithOpenApi();

        permissionsGroup.MapPost("/", CreatePermission).WithName("CreatePermission").WithOpenApi();

        permissionsGroup.MapPut("/{id:guid}", UpdatePermission).WithName("UpdatePermission").WithOpenApi();

        permissionsGroup.MapDelete("/{id:guid}", DeletePermission).WithName("DeletePermission").WithOpenApi();

        // Role-Permission associations
        permissionsGroup.MapPost("/{roleId:guid}/permissions", AssignPermissionsToRole).WithName("AssignPermissionsToRole").WithOpenApi();

        permissionsGroup.MapDelete("/{roleId:guid}/permissions/{permissionId:guid}", RemovePermissionFromRole).WithName("RemovePermissionFromRole").WithOpenApi();

        // Also map without /api prefix for backward compatibility with tests
        var permissionsGroupNoApi = app.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();

        permissionsGroupNoApi.MapGet("/", GetAllPermissions).WithName("GetAllPermissionsNoApi").WithOpenApi();

        permissionsGroupNoApi.MapGet("/{id:guid}", GetPermissionById).WithName("GetPermissionByIdNoApi").WithOpenApi();

        permissionsGroupNoApi.MapPost("/", CreatePermission).WithName("CreatePermissionNoApi").WithOpenApi();

        permissionsGroupNoApi.MapPut("/{id:guid}", UpdatePermission).WithName("UpdatePermissionNoApi").WithOpenApi();

        permissionsGroupNoApi.MapDelete("/{id:guid}", DeletePermission).WithName("DeletePermissionNoApi").WithOpenApi();

        return app;
    }

    private static IResult GetAllPermissions(HttpContext context)
    {
        return Results.Ok(PermissionsEndpointCatalog.List());
    }

    private static IResult GetPermissionById(Guid id, HttpContext context)
    {
        var permission = PermissionsEndpointCatalog.GetById(id);
        return permission is null
            ? Results.NotFound(new { message = "Permission not found" })
            : Results.Ok(permission);
    }

    private static IResult CreatePermission([FromBody] CreatePermissionRequest request, HttpContext context)
    {
        return PermissionDefinitionsAreCodeDefined();
    }

    private static IResult UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request, HttpContext context)
    {
        return PermissionDefinitionsAreCodeDefined();
    }

    private static IResult DeletePermission(Guid id, HttpContext context)
    {
        return PermissionDefinitionsAreCodeDefined();
    }

    private static IResult AssignPermissionsToRole(Guid roleId, [FromBody] AssignPermissionsRequest request, HttpContext context)
    {
        var validation = PermissionsEndpointCatalog.ValidatePermissionIds(request.PermissionIds);
        if (validation.InvalidPermissionIds.Count > 0)
        {
            return Results.BadRequest(new
            {
                message = "One or more permission IDs are not registered.",
                invalidPermissionIds = validation.InvalidPermissionIds,
            });
        }

        return Results.Problem(
            title: "Role-permission assignment uses tenant authorization APIs",
            detail: "Dynamic role assignment is managed by the Identity.Authorization tenant permission and role APIs. This legacy shell endpoint now rejects unsupported writes explicitly.",
            statusCode: StatusCodes.Status405MethodNotAllowed);
    }

    private static IResult RemovePermissionFromRole(Guid roleId, Guid permissionId, HttpContext context)
    {
        return PermissionsEndpointCatalog.GetById(permissionId) is null
            ? Results.NotFound(new { message = "Permission not found" })
            : Results.Problem(
                title: "Role-permission assignment uses tenant authorization APIs",
                detail: "Dynamic role assignment is managed by the Identity.Authorization tenant permission and role APIs. This legacy shell endpoint now rejects unsupported writes explicitly.",
                statusCode: StatusCodes.Status405MethodNotAllowed);
    }

    private static IResult PermissionDefinitionsAreCodeDefined()
        => Results.Problem(
            title: "Permission definitions are code-defined",
            detail: "Permissions are discovered from strongly typed Permission classes in Identity.Authorization. Add or change permission definitions in code, then deploy the API.",
            statusCode: StatusCodes.Status405MethodNotAllowed);
}

/// <summary>
///     Request DTO for creating a permission
/// </summary>
public sealed record CreatePermissionRequest(string Name, string? Description, string? Resource);

/// <summary>
///     Request DTO for updating a permission
/// </summary>
public sealed record UpdatePermissionRequest(string Name, string? Description, string? Resource);

/// <summary>
///     Request DTO for assigning permissions to a role
/// </summary>
public sealed record AssignPermissionsRequest(Guid[ ] PermissionIds);

/// <summary>
///     DTO for permission response data
/// </summary>
public class PermissionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Resource { get; set; }

    public string? Action { get; set; }

    public string? Scope { get; set; }
}

public sealed record PermissionValidationResult(
    IReadOnlyList<PermissionDto> ValidPermissions,
    IReadOnlyList<Guid> InvalidPermissionIds);

public static class PermissionsEndpointCatalog
{
    public static IReadOnlyList<PermissionDto> List()
        => PermissionRegistry.Permissions.Values
            .OrderBy(permission => permission.Resource)
            .ThenBy(permission => permission.Action)
            .ThenBy(permission => permission.Scope)
            .Select(ToDto)
            .ToList();

    public static PermissionDto? GetById(Guid id)
        => List().FirstOrDefault(permission => permission.Id == id);

    public static PermissionValidationResult ValidatePermissionIds(IEnumerable<Guid> permissionIds)
    {
        var registered = List().ToDictionary(permission => permission.Id);
        var valid = new List<PermissionDto>();
        var invalid = new List<Guid>();

        foreach (var permissionId in permissionIds.Distinct())
        {
            if (registered.TryGetValue(permissionId, out var permission))
            {
                valid.Add(permission);
            }
            else
            {
                invalid.Add(permissionId);
            }
        }

        return new PermissionValidationResult(valid, invalid);
    }

    public static Guid GetStableId(string permissionKey)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"gameguild:permission:{permissionKey.ToLowerInvariant()}"));
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x30);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static PermissionDto ToDto(GameGuild.Identity.Authorization.Models.Permission permission)
        => new()
        {
            Id = GetStableId(permission.Key),
            Name = permission.Key,
            Description = permission.Description,
            Resource = permission.Resource,
            Action = permission.Action,
            Scope = permission.Scope,
        };
}
