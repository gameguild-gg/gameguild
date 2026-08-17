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
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var tenants = app.MapGroup("/tenants").WithTags("Tenants").WithOpenApi();

        tenants.MapGet("/", TenantsEndpointHandlers.GetTenants)
            .WithName("GetTenants")
            .WithSummary("Get all tenants")
            .WithDescription("Retrieves a list of all tenants in the system.")
            .Produces<ReadOnlyCollection<TenantResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapGet("/{tenantId:guid}", TenantsEndpointHandlers.GetTenant)
            .WithName("GetTenant")
            .WithSummary("Get tenant by ID")
            .WithDescription("Retrieves a specific tenant by its unique identifier.")
            .Produces<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapPost("/", TenantsEndpointHandlers.CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Create a new tenant")
            .WithDescription("Creates a new tenant with the provided information.")
            .Accepts<CreateTenantRequest>(MediaTypeNames.Application.Json)
            .Produces<TenantResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapPut("/{tenantId:guid}", TenantsEndpointHandlers.UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Update an existing tenant")
            .WithDescription("Updates an existing tenant with the provided information.")
            .Accepts<UpdateTenantRequest>(MediaTypeNames.Application.Json)
            .Produces<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapDelete("/{tenantId:guid}", TenantsEndpointHandlers.DeleteTenant)
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
    public static async Task<IResult> GetTenants([FromServices] ISender sender, [FromQuery] int page = 1, [FromQuery] int pageSize = 500, CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 500 ? 500 : pageSize;
        var tenants = await sender.Send(
            new GetTenantsPageQuery(normalizedPage, normalizedPageSize),
            cancellationToken).ConfigureAwait(false);

        return Results.Ok(new ReadOnlyCollection<TenantResponse>(tenants.Items.Select(tenant => ToResponse(tenant)).ToList()));
    }

    public static async Task<IResult> GetTenant(Guid id, [FromServices] ISender sender, CancellationToken cancellationToken = default)
    {
        var tenant = await sender.Send(new GetTenantByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return tenant is null ? Results.NotFound() : Results.Ok(ToResponse(tenant));
    }

    public static async Task<IResult> CreateTenant([FromBody] CreateTenantRequest request, [FromServices] ISender sender, CancellationToken cancellationToken = default)
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
            : Results.Created($"/tenants/{tenant.Id}", ToResponse(tenant));
    }

    public static async Task<IResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request, [FromServices] ISender sender, CancellationToken cancellationToken = default)
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
        return tenant is null ? Results.NotFound() : Results.Ok(ToResponse(tenant));
    }

    public static async Task<IResult> DeleteTenant(Guid id, [FromServices] ISender sender, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ArchiveTenantCommand(id, "Deleted through legacy /tenants endpoint"),
            cancellationToken).ConfigureAwait(false);

        return result.Success ? Results.NoContent() : Results.NotFound(new { message = result.Message });
    }

    private static TenantResponse ToResponse(Tenant tenant)
        => new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAt);
}

public sealed record TenantResponse(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);

public sealed record CreateTenantRequest(
    [Required] string Name,
    [Required] string Slug,
    [EmailAddress] string? AdminEmail = null,
    string? Description = null);

public sealed record UpdateTenantRequest(
    [Required] string Name,
    bool IsActive,
    string? Description = null);
