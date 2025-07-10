using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Common.Models;

namespace GameGuild.Modules.Users;

/// <summary>
/// Query to get user by ID
/// </summary>
public sealed class GetUserByIdQuery : IQuery<User?>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Query to get user by email
/// </summary>
public sealed class GetUserByEmailQuery : IQuery<User?>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Query to search users with filtering and pagination
/// </summary>
public sealed class SearchUsersQuery : PaginatedQuery<User>
{
    public bool? IsActive { get; set; }
    
    public decimal? MinBalance { get; set; }
    
    public decimal? MaxBalance { get; set; }

    public DateTime? CreatedAfter { get; set; }

    public DateTime? CreatedBefore { get; set; }

    public DateTime? UpdatedAfter { get; set; }

    public DateTime? UpdatedBefore { get; set; }

    /// <summary>
    /// Sort field options
    /// </summary>
    public UserSortField SortBy { get; set; } = UserSortField.UpdatedAt;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Query to get user statistics
/// </summary>
public sealed class GetUserStatisticsQuery : IQuery<UserStatistics>
{
    public DateTime? FromDate { get; set; }
    
    public DateTime? ToDate { get; set; }
    
    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Query to get users with low balance
/// </summary>
public sealed class GetUsersWithLowBalanceQuery : PaginatedQuery<User>
{
    [Range(0, double.MaxValue)]
    public decimal ThresholdBalance { get; set; } = 10.0m;
}

/// <summary>
/// User statistics result
/// </summary>
public class UserStatistics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int DeletedUsers { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal AverageBalance { get; set; }
    public int UsersCreatedToday { get; set; }
    public int UsersCreatedThisWeek { get; set; }
    public int UsersCreatedThisMonth { get; set; }
}
