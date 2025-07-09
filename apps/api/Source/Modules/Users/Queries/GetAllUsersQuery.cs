using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Behaviors;
using MediatR;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
/// Query to get all users with optional filtering, pagination, and caching
/// </summary>
public class GetAllUsersQuery : IRequest<IEnumerable<Models.User>>, ICachedRequest
{
    public bool IncludeDeleted { get; set; } = false;

    public bool? IsActive { get; set; }

    public int Skip { get; set; } = 0;

    [Range(1, 100)]
    public int Take { get; set; } = 50;

    // Caching implementation
    public string CacheKey => $"users:all:{IncludeDeleted}:{IsActive}:{Skip}:{Take}";
    
    public TimeSpan CacheExpiration => TimeSpan.FromMinutes(10);
    
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(2);
}
