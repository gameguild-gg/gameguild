using System.Security.Claims;
using GameGuild.Authorization;
using GameGuild.Authorization.Identity;
using GameGuild.GraphQL;
using GameGuild.Modules.Programs;
using GameGuild.Source.Modules.Programs.Models;
using Microsoft.Extensions.Logging;
using ProgramEntity = GameGuild.Modules.Programs.Program;

namespace GameGuild.Source.Modules.Programs.GraphQL;

/// <summary> GraphQL queries for Program module using proper authorization and user access </summary>
[ExtendObjectType<Query>]
public class ProgramQueries {

    /// <summary> Gets all programs the current user can edit (owned/has permissions for) </summary>
    [HotChocolate.Authorization.Authorize] // Requires authentication
    public async Task<IEnumerable<ProgramEntity>> GetMyPrograms(
      ClaimsPrincipal claimsPrincipal,
      [Service] IProgramService programService,
      [Service] ILogger<ProgramQueries> logger,
      int skip = 0,
      int take = 50
    ) {
        logger.LogInformation("=== GetMyPrograms Debug ===");
        logger.LogInformation("Claims Principal is null: {IsNull}", claimsPrincipal == null);
        logger.LogInformation("Claims Principal Identity Authenticated: {IsAuthenticated}", claimsPrincipal?.Identity?.IsAuthenticated);
        logger.LogInformation("Total claims count: {ClaimsCount}", claimsPrincipal?.Claims?.Count() ?? 0);

        if (claimsPrincipal?.Claims != null) {
            foreach (var claim in claimsPrincipal.Claims) {
                logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }
        }

        try {
            // Try to get user ID using different methods
            var subClaim = claimsPrincipal?.FindFirst("sub")?.Value;
            var nameIdentifierClaim = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userIdClaim = claimsPrincipal?.FindFirst("user_id")?.Value;

            logger.LogInformation("Sub claim: {SubClaim}", subClaim);
            logger.LogInformation("NameIdentifier claim: {NameIdentifierClaim}", nameIdentifierClaim);
            logger.LogInformation("UserId claim: {UserIdClaim}", userIdClaim);

            // Use the robust extension method
            var userId = claimsPrincipal.GetUserId();
            logger.LogInformation("GetUserId() result: {UserId}", userId);

            if (string.IsNullOrEmpty(userId)) {
                logger.LogWarning("No valid user ID found in claims");
                return Enumerable.Empty<ProgramEntity>();
            }

            // Convert string userId to Guid
            if (!Guid.TryParse(userId, out var userGuid)) {
                logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return Enumerable.Empty<ProgramEntity>();
            }

            // Get programs for the user
            var programs = await programService.GetProgramsByCreatorAsync(userGuid, skip, take);
            logger.LogInformation("Found {ProgramCount} programs for user {UserId}", programs.Count(), userId);

            return programs;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error in GetMyPrograms");
            throw;
        }
    }

    /// <summary> Gets all public programs (no auth required) </summary>
    public async Task<IEnumerable<ProgramEntity>> GetPublishedPrograms(
      [Service] IProgramService programService,
      int skip = 0,
      int take = 50
    ) {
        return await programService.GetPublishedProgramsAsync(skip, take);
    }

    /// <summary> Gets a program by ID (with proper authorization) </summary>
    [GraphQLRequireResourcePermission<ProgramPermission, ProgramEntity>(PermissionType.Read, "id")]
    public async Task<ProgramEntity?> GetProgramById(
      Guid id,
      [Service] IProgramService programService
    ) {
        return await programService.GetByIdAsync(id);
    }

    /// <summary> Gets a program by slug (public access for published programs) </summary>
    public async Task<ProgramEntity?> GetProgramBySlug(
      string slug,
      [Service] IProgramService programService
    ) {
        return await programService.GetPublishedProgramBySlugAsync(slug);
    }

    /// <summary> Test resolver without authorization to check if auth pipeline is the issue </summary>
    public Task<string> TestAuth(ClaimsPrincipal claimsPrincipal, [Service] ILogger<ProgramQueries> logger) {
        logger.LogInformation("=== TestAuth Debug ===");
        logger.LogInformation("Claims Principal is null: {IsNull}", claimsPrincipal == null);
        logger.LogInformation("Claims Principal Identity Authenticated: {IsAuthenticated}", claimsPrincipal?.Identity?.IsAuthenticated);
        return Task.FromResult($"Auth test - Authenticated: {claimsPrincipal?.Identity?.IsAuthenticated}, Claims: {claimsPrincipal?.Claims?.Count() ?? 0}");
    }
}
