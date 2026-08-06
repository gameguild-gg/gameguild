using System.Globalization;
using System.Text;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Query handler for getting users with cursor-based pagination, filtering, and search
/// </summary>
public sealed class GetUsersQueryHandler(
    IUserRepository userRepository,
    IActorContextAccessor actorContextAccessor,
    ITenantMemberRepository tenantMemberRepository) : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Decode cursor if provided
        DateTime? cursorTimestamp = null;
        Guid? cursorId = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            try
            {
                var cursorData = DecodeCursor(request.Cursor);
                cursorTimestamp = cursorData.Timestamp;
                cursorId = cursorData.Id;
            }
            catch
            {
                // Invalid cursor, ignore it
            }
        }

        // Start with queryable for database-level filtering
        IQueryable<User> query = userRepository.GetQueryable();

        var actorTenantId = Actor.TenantId;

        if (!Actor.IsSystemAdmin)
        {
            if (!actorTenantId.HasValue)
            {
                var unauthenticatedActorId = Actor.SubjectIdAsGuid
                    ?? throw new AuthenticationRequiredException("Authenticated user ID is required to list users.");

                throw new AccessDeniedException($"User {unauthenticatedActorId} attempted to list users without tenant context.");
            }

            var actorUserId = Actor.SubjectIdAsGuid
                ?? throw new AuthenticationRequiredException("Authenticated user ID is required to list users.");

            var actorMembership = await tenantMemberRepository
                .GetByUserAndTenantAsync(actorUserId, actorTenantId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (actorMembership is null || !actorMembership.IsActive)
            {
                throw AccessDeniedException.ForTenantMembership(actorUserId, actorTenantId.Value);
            }

            query = query.Where(u => u.TenantMemberships.Any(m => m.TenantId == actorTenantId.Value && m.IsActive));
        }

        // Apply includeDeleted filter first
        if (!request.IncludeDeleted)
        {
            query = query.Where(u => u.DeletedAt == null);
        }

        // Apply email filter
        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(u => u.Email.ToLower() == request.Email.ToLower());
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = request.Status.ToLowerInvariant() switch
            {
                "active" => query.Where(u => u.IsActive && u.DeletedAt == null),
                "inactive" => query.Where(u => !u.IsActive && u.DeletedAt == null),
                "deleted" => query.Where(u => u.DeletedAt != null),
                _ => query
            };
        }

        // Apply search term
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(searchTerm) || u.Email.ToLower().Contains(searchTerm));
        }

        // Apply sorting
        var sortField = request.Sort?.TrimStart('-') ?? "created_at";
        var sortDescending = request.Sort?.StartsWith('-') ?? false;

        query = sortField.ToLowerInvariant() switch
        {
            "email" => sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "name" => sortDescending ? query.OrderByDescending(u => u.Name) : query.OrderBy(u => u.Name),
            "updated_at" => sortDescending ? query.OrderByDescending(u => u.UpdatedAt) : query.OrderBy(u => u.UpdatedAt),
            _ => sortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        // Apply cursor pagination
        if (cursorTimestamp.HasValue && cursorId.HasValue)
        {
            // Skip to items after the cursor
            query = sortDescending
                ? query.Where(u => u.CreatedAt < cursorTimestamp.Value || (u.CreatedAt == cursorTimestamp.Value && u.Id.CompareTo(cursorId.Value) < 0))
                : query.Where(u => u.CreatedAt > cursorTimestamp.Value || (u.CreatedAt == cursorTimestamp.Value && u.Id.CompareTo(cursorId.Value) > 0));
        }

        // Get total count at database level
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        // Apply limit and execute query at database level
        var users = await query.Take(request.Limit).ToListAsync(cancellationToken).ConfigureAwait(false);

        // Map to DTOs
        var userDtos = users.Select(user => new UserDto(
            user.Id,
            user.Email,
            user.Name,
            user.CreatedAt,
            user.UpdatedAt,
            user.IsActive,
            user.PhoneNumber,
            user.LastSeenAt
        )).ToList();

        // Note: For cursor-based pagination, we use PageNumber=1 and PageSize=Limit
        // The cursor itself handles the "page" concept
        return PagedResult<UserDto>.FromPage(userDtos, totalCount, 1, request.Limit);
    }

    /// <summary>
    ///     Decodes a cursor string to extract timestamp and ID
    /// </summary>
    private static (DateTime Timestamp, Guid Id) DecodeCursor(string cursor)
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = decoded.Split('|');

        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid cursor format");
        }

        var timestamp = DateTime.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var id = Guid.Parse(parts[1]);

        return (timestamp, id);
    }

    /// <summary>
    ///     Encodes timestamp and ID into a cursor string
    /// </summary>
    public static string EncodeCursor(DateTime timestamp, Guid id)
    {
        var value = $"{timestamp:O}|{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }
}
