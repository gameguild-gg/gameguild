using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
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

        tenants.MapGet("/", GetTenants)
            .WithName("GetTenants")
            .WithSummary("Get all tenants")
            .WithDescription("Retrieves a list of all tenants in the system.")
            .Produces<ReadOnlyCollection<TenantResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapGet("/{id:guid}", GetTenant)
            .WithName("GetTenant")
            .WithSummary("Get tenant by ID")
            .WithDescription("Retrieves a specific tenant by its unique identifier.")
            .Produces<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Create a new tenant")
            .WithDescription("Creates a new tenant with the provided information.")
            .Accepts<CreateTenantRequest>(MediaTypeNames.Application.Json)
            .Produces<TenantResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Update an existing tenant")
            .WithDescription("Updates an existing tenant with the provided information.")
            .Accepts<UpdateTenantRequest>(MediaTypeNames.Application.Json)
            .Produces<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        tenants.MapDelete("/{id:guid}", DeleteTenant)
            .WithName("DeleteTenant")
            .WithSummary("Delete a tenant")
            .WithDescription("Deletes a tenant and all associated data.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }

    /// <summary>
    ///     Gets all tenants.
    /// </summary>
    /// <returns>A collection of tenant responses</returns>
    private static Task<IResult> GetTenants()
    {
        var tenants = new List<TenantResponse>
        {
            new TenantResponse(Guid.NewGuid(), "Acme Corp", "acme-corp", "Enterprise", true, SystemClock.UtcNow.AddDays(-30)),
            new TenantResponse(Guid.NewGuid(), "Beta Inc", "beta-inc", "Professional", true, SystemClock.UtcNow.AddDays(-15))
        };

        return Task.FromResult(Results.Ok(new ReadOnlyCollection<TenantResponse>(tenants)));
    }

    /// <summary>
    ///     Gets a tenant by ID.
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <returns>The tenant response or not found</returns>
    private static Task<IResult> GetTenant(Guid id)
    {
        var tenant = new TenantResponse(id, "Acme Corp", "acme-corp", "Enterprise", true, SystemClock.UtcNow.AddDays(-30));

        return Task.FromResult(Results.Ok(tenant));
    }

    /// <summary>
    ///     Creates a new tenant.
    /// </summary>
    /// <param name="request">The create tenant request</param>
    /// <returns>The created tenant response</returns>
    private static Task<IResult> CreateTenant(CreateTenantRequest request)
    {
        var tenant = new TenantResponse(Guid.NewGuid(), request.Name, request.Slug, request.Plan, true, SystemClock.UtcNow);

        return Task.FromResult(Results.Created($"/tenants/{tenant.Id}", tenant));
    }

    /// <summary>
    ///     Updates an existing tenant.
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="request">The update tenant request</param>
    /// <returns>The updated tenant response</returns>
    private static Task<IResult> UpdateTenant(Guid id, UpdateTenantRequest request)
    {
        var tenant = new TenantResponse(id, request.Name, request.Slug, request.Plan, request.IsActive, SystemClock.UtcNow.AddDays(-30));

        return Task.FromResult(Results.Ok(tenant));
    }

    /// <summary>
    ///     Deletes a tenant.
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <returns>No content response</returns>
    private static Task<IResult> DeleteTenant(Guid id) { return Task.FromResult(Results.NoContent()); }
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
internal record TenantResponse(Guid Id, string Name, string Slug, string Plan, bool IsActive, DateTime CreatedAt);

/// <summary>
///     Represents a request to create a new tenant.
/// </summary>
/// <param name="Name">The tenant name</param>
/// <param name="Slug">The tenant URL slug</param>
/// <param name="Plan">The subscription plan</param>
internal record CreateTenantRequest([Required] string Name, [Required] string Slug, [Required] string Plan);

/// <summary>
///     Represents a request to update an existing tenant.
/// </summary>
/// <param name="Name">The tenant name</param>
/// <param name="Slug">The tenant URL slug</param>
/// <param name="Plan">The subscription plan</param>
/// <param name="IsActive">Whether the tenant is active</param>
internal record UpdateTenantRequest([Required] string Name, [Required] string Slug, [Required] string Plan, bool IsActive);
