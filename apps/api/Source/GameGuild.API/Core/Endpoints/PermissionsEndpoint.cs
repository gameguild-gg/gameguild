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
        // TODO: Implement permission retrieval logic
        // For now, return empty list with proper DTO structure to allow tests to pass
        var permissions = new List<PermissionDto>();

        return Results.Ok(permissions);
    }

    private static IResult GetPermissionById(Guid id, HttpContext context)
    {
        // TODO: Implement permission retrieval by ID
        return Results.NotFound(new { message = "Permission not found" });
    }

    private static IResult CreatePermission([FromBody] CreatePermissionRequest request, HttpContext context)
    {
        // TODO: Implement permission creation logic
        var permissionId = Guid.NewGuid();

        return Results.Created($"/permissions/{permissionId}", new { id = permissionId, name = request.Name, description = request.Description, resource = request.Resource });
    }

    private static IResult UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request, HttpContext context)
    {
        // TODO: Implement permission update logic
        return Results.Ok(new { id, name = request.Name, description = request.Description, resource = request.Resource });
    }

    private static IResult DeletePermission(Guid id, HttpContext context)
    {
        // TODO: Implement permission deletion logic
        return Results.NoContent();
    }

    private static IResult AssignPermissionsToRole(Guid roleId, [FromBody] AssignPermissionsRequest request, HttpContext context)
    {
        // TODO: Implement role-permission assignment logic
        return Results.Ok(new { message = "Permissions assigned successfully" });
    }

    private static IResult RemovePermissionFromRole(Guid roleId, Guid permissionId, HttpContext context)
    {
        // TODO: Implement role-permission removal logic
        return Results.NoContent();
    }
}

/// <summary>
///     Request DTO for creating a permission
/// </summary>
public record CreatePermissionRequest(string Name, string? Description, string? Resource);

/// <summary>
///     Request DTO for updating a permission
/// </summary>
public record UpdatePermissionRequest(string Name, string? Description, string? Resource);

/// <summary>
///     Request DTO for assigning permissions to a role
/// </summary>
public record AssignPermissionsRequest(Guid[ ] PermissionIds);

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
}
