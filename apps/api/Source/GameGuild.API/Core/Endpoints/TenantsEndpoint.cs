using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Tenant management endpoints for multi-tenant operations.
/// </summary>
internal class TenantsEndpoint : IEndpoint
{
    /// <summary>
    ///     Maps the tenant endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var tenants = app.MapGroup("/tenants").WithTags("Tenants").WithOpenApi();

        tenants.MapGet("/", ([FromServices] ISender sender, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
                TenantsEndpointHandlers.GetTenants(sender, page, pageSize, cancellationToken))
            .WithName("GetTenants")
            .WithSummary("Get all tenants")
            .WithDescription("Retrieves a list of all tenants in the system.")
            .Produces<ReadOnlyCollection<TenantResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapGet("/{id:guid}", (Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                TenantsEndpointHandlers.GetTenant(id, sender, cancellationToken))
            .WithName("GetTenant")
            .WithSummary("Get tenant by ID")
            .WithDescription("Retrieves a specific tenant by its unique identifier.")
            .Produces<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapPost("/", ([FromBody] CreateTenantRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                TenantsEndpointHandlers.CreateTenant(request, sender, cancellationToken))
            .WithName("CreateTenant")
            .WithSummary("Create a new tenant")
            .WithDescription("Creates a new tenant with the provided information.")
            .Accepts<CreateTenantRequest>(MediaTypeNames.Application.Json)
            .Produces<TenantResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapPut("/{id:guid}", (Guid id, [FromBody] UpdateTenantRequest request, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                TenantsEndpointHandlers.UpdateTenant(id, request, sender, cancellationToken))
            .WithName("UpdateTenant")
            .WithSummary("Update an existing tenant")
            .WithDescription("Updates an existing tenant with the provided information.")
            .Accepts<UpdateTenantRequest>(MediaTypeNames.Application.Json)
            .Produces<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapDelete("/{id:guid}", (Guid id, [FromServices] ISender sender, CancellationToken cancellationToken) =>
                TenantsEndpointHandlers.DeleteTenant(id, sender, cancellationToken))
            .WithName("DeleteTenant")
            .WithSummary("Delete a tenant")
            .WithDescription("Deletes a tenant and all associated data.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

}

/// <summary>
///     CQRS-backed handlers for the legacy shell tenant endpoints.
/// </summary>
public static class TenantsEndpointHandlers
{
    public static async Task<IResult> GetTenants(ISender sender, int page = 1, int pageSize = 500, CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 500 ? 500 : pageSize;
        var tenants = await sender.Send(
            new GetTenantsPageQuery(normalizedPage, normalizedPageSize),
            cancellationToken).ConfigureAwait(false);

        return Results.Ok(new ReadOnlyCollection<TenantResponse>(tenants.Items.Select(tenant => ToResponse(tenant)).ToList()));
    }

    public static async Task<IResult> GetTenant(Guid id, ISender sender, CancellationToken cancellationToken = default)
    {
        var tenant = await sender.Send(new GetTenantByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return tenant is null ? Results.NotFound() : Results.Ok(ToResponse(tenant));
    }

    public static async Task<IResult> CreateTenant(CreateTenantRequest request, ISender sender, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.AdminEmail))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.AdminEmail)] = ["Admin email is required to create a tenant."]
            });
        }

        var tenantId = await sender.Send(
            new CreateTenantCommand(request.Name, request.Slug, request.AdminEmail, request.Description),
            cancellationToken).ConfigureAwait(false);
        var tenant = await sender.Send(new GetTenantByIdQuery(tenantId), cancellationToken).ConfigureAwait(false);

        return tenant is null
            ? Results.Created($"/tenants/{tenantId}", new { id = tenantId })
            : Results.Created($"/tenants/{tenant.Id}", ToResponse(tenant, request.Plan));
    }

    public static async Task<IResult> UpdateTenant(Guid id, UpdateTenantRequest request, ISender sender, CancellationToken cancellationToken = default)
    {
        await sender.Send(new UpdateTenantCommand(id, request.Name, request.Description), cancellationToken).ConfigureAwait(false);

        if (request.IsActive)
        {
            await sender.Send(new ActivateTenantCommand(id), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await sender.Send(new DeactivateTenantCommand(id), cancellationToken).ConfigureAwait(false);
        }

        var tenant = await sender.Send(new GetTenantByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return tenant is null ? Results.NotFound() : Results.Ok(ToResponse(tenant, request.Plan));
    }

    public static async Task<IResult> DeleteTenant(Guid id, ISender sender, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ArchiveTenantCommand(id, "Deleted through legacy /tenants endpoint"),
            cancellationToken).ConfigureAwait(false);

        return result.Success ? Results.NoContent() : Results.NotFound(new { message = result.Message });
    }

    private static TenantResponse ToResponse(Tenant tenant, string? plan = null)
        => new(tenant.Id, tenant.Name, tenant.Slug, plan, tenant.IsActive, tenant.CreatedAt);
}

/// <summary>
///     Represents a tenant response.
/// </summary>
/// <param name="Id">The unique tenant identifier</param>
/// <param name="Name">The tenant name</param>
/// <param name="Slug">The tenant URL slug</param>
/// <param name="Plan">The subscription plan</param>
/// <param name="IsActive">Whether the tenant is active</param>
/// <param name="CreatedAt">When the tenant was created</param>
public sealed record TenantResponse(Guid Id, string Name, string Slug, string? Plan, bool IsActive, DateTime CreatedAt);

/// <summary>
///     Represents a request to create a new tenant.
/// </summary>
/// <param name="Name">The tenant name</param>
/// <param name="Slug">The tenant URL slug</param>
/// <param name="Plan">The subscription plan</param>
/// <param name="AdminEmail">The tenant administrator email address</param>
/// <param name="Description">Optional tenant description</param>
public sealed record CreateTenantRequest(
    [Required] string Name,
    [Required] string Slug,
    string? Plan = null,
    [EmailAddress] string? AdminEmail = null,
    string? Description = null);

/// <summary>
///     Represents a request to update an existing tenant.
/// </summary>
/// <param name="Name">The tenant name</param>
/// <param name="Slug">The tenant URL slug</param>
/// <param name="Plan">The subscription plan</param>
/// <param name="IsActive">Whether the tenant is active</param>
/// <param name="Description">Optional tenant description</param>
public sealed record UpdateTenantRequest(
    [Required] string Name,
    [Required] string Slug,
    string? Plan,
    bool IsActive,
    string? Description = null);
