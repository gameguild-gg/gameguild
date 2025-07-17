using GameGuild.Common;
using GameGuild.Modules.Tenants.Inputs;
using MediatR;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// GraphQL queries for Tenant module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class TenantQueries {
  /// <summary>
  /// Get all tenants (non-deleted only)
  /// </summary>
  public async Task<IEnumerable<Tenant>> GetTenants(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) { throw new GraphQLException("Authentication required to access tenant data."); }

    var query = new GetAllTenantsQuery(false);
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.ToString());

    return result.Value;
  }

  /// <summary>
  /// Get a tenant by ID
  /// </summary>
  public async Task<Tenant?> GetTenantById(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor,
    Guid id
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new GetTenantByIdQuery(id, false);
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.ToString());

    return result.Value;
  }

  /// <summary>
  /// Get a tenant by name
  /// </summary>
  public async Task<Tenant?> GetTenantByName(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor,
    string name
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new GetTenantByNameQuery(name, false);
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.ToString());

    return result.Value;
  }

  /// <summary>
  /// Get a tenant by slug
  /// </summary>
  public async Task<Tenant?> GetTenantBySlug(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor,
    string slug
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new GetTenantBySlugQuery(slug, false);
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.Description);

    return result.Value;
  }

  /// <summary>
  /// Get soft-deleted tenants
  /// </summary>
  public async Task<IEnumerable<Tenant>> GetDeletedTenants(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new GetDeletedTenantsQuery();
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.Description);

    return result.Value;
  }

  /// <summary>
  /// Get active tenants
  /// </summary>
  public async Task<IEnumerable<Tenant>> GetActiveTenants(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new GetActiveTenantsQuery();
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.Description);

    return result.Value;
  }

  /// <summary>
  /// Search tenants with advanced filtering
  /// </summary>
  public async Task<IEnumerable<Tenant>> SearchTenants(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor,
    SearchTenantsInput input
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new SearchTenantsQuery(
      input.SearchTerm,
      input.IsActive,
      input.IncludeDeleted,
      input.SortBy,
      input.SortDescending,
      input.Limit,
      input.Offset
    );
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.Description);

    return result.Value;
  }

  /// <summary>
  /// Get tenant statistics
  /// </summary>
  public async Task<TenantStatistics> GetTenantStatistics(
    [Service] IMediator mediator,
    [Service] IHttpContextAccessor httpContextAccessor
  ) {
    // Require authentication for tenant queries
    var httpContext = httpContextAccessor.HttpContext;

    if (httpContext == null || !httpContext.User.Identity?.IsAuthenticated == true) throw new UnauthorizedAccessException("Authentication required");

    var query = new GetTenantStatisticsQuery();
    var result = await mediator.Send(query);

    if (result.IsFailure) throw new GraphQLException(result.ErrorMessage.Description);

    return result.Value;
  }
}
