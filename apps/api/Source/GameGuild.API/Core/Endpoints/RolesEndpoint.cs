using GameGuild.CQRS;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Mvc;
using RoleDto = GameGuild.Identity.Authentication.RoleDto;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Endpoints for role management
/// </summary>
public static class RolesEndpoint
{
    /// <summary>
    ///     Maps role management endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var rolesGroup = app.MapGroup("/roles").WithTags("Roles").RequireAuthorization();

        rolesGroup.MapGet("/", (Delegate)GetAllRoles).WithName("GetAllRoles").WithOpenApi();

        rolesGroup.MapGet("/{id:guid}", GetRoleById).WithName("GetRoleById").WithOpenApi();

        rolesGroup.MapPost("/", CreateRole).WithName("CreateRole").WithOpenApi();

        rolesGroup.MapPut("/{id:guid}", UpdateRole).WithName("UpdateRole").WithOpenApi();

        rolesGroup.MapDelete("/{id:guid}", DeleteRole).WithName("DeleteRole").WithOpenApi();

        // Also map without /api prefix for backward compatibility with tests
        var rolesGroupNoApi = app.MapGroup("/roles").WithTags("Roles").RequireAuthorization();

        rolesGroupNoApi.MapGet("/", (Delegate)GetAllRoles).WithName("GetAllRolesNoApi").WithOpenApi();

        rolesGroupNoApi.MapGet("/{id:guid}", GetRoleById).WithName("GetRoleByIdNoApi").WithOpenApi();

        rolesGroupNoApi.MapPost("/", CreateRole).WithName("CreateRoleNoApi").WithOpenApi();

        rolesGroupNoApi.MapPut("/{id:guid}", UpdateRole).WithName("UpdateRoleNoApi").WithOpenApi();

        rolesGroupNoApi.MapDelete("/{id:guid}", DeleteRole).WithName("DeleteRoleNoApi").WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetAllRoles(HttpContext context)
    {
        var sender = context.RequestServices.GetRequiredService<ISender>();
        var query = new GetRolesQuery { IncludeInactive = false };
        var roles = await sender.Send<List<RoleDto>>(query).ConfigureAwait(false);

        return Results.Ok(roles);
    }

    private static async Task<IResult> GetRoleById(Guid id, HttpContext context)
    {
        var sender = context.RequestServices.GetRequiredService<ISender>();
        var query = new GetRoleByIdQuery { RoleId = id };
        var role = await sender.Send<RoleDto?>(query).ConfigureAwait(false);

        return role is null
            ? Results.NotFound(new { message = "Role not found" })
            : Results.Ok(role);
    }

    private static async Task<IResult> CreateRole([FromBody] CreateRoleEndpointRequest request, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Role name is required" });
        }

        var sender = context.RequestServices.GetRequiredService<ISender>();
        var command = new CreateRoleCommand
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Permissions = request.Permissions?.Select(p => p.ToString()).ToList() ?? new List<string>()
        };
        var role = await sender.Send<RoleDto>(command).ConfigureAwait(false);

        return Results.Created($"/roles/{role.Id}", role);
    }

    private static async Task<IResult> UpdateRole(Guid id, [FromBody] UpdateRoleEndpointRequest request, HttpContext context)
    {
        var sender = context.RequestServices.GetRequiredService<ISender>();
        var command = new UpdateRoleCommand
        {
            RoleId = id,
            Name = request.Name,
            Description = request.Description,
            Permissions = request.Permissions?.Select(p => p.ToString()).ToList()
        };
        var role = await sender.Send<RoleDto>(command).ConfigureAwait(false);

        return Results.Ok(role);
    }

    private static async Task<IResult> DeleteRole(Guid id, HttpContext context)
    {
        var sender = context.RequestServices.GetRequiredService<ISender>();
        var command = new DeleteRoleCommand { RoleId = id };
        var result = await sender.Send<bool>(command).ConfigureAwait(false);

        return result ? Results.NoContent() : Results.NotFound(new { message = "Role not found" });
    }
}

/// <summary>
///     Request DTO for creating a role (endpoint-level)
/// </summary>
public sealed record CreateRoleEndpointRequest(string Name, string? Description, Guid[]? Permissions);

/// <summary>
///     Request DTO for updating a role (endpoint-level)
/// </summary>
public sealed record UpdateRoleEndpointRequest(string Name, string? Description, Guid[]? Permissions);
