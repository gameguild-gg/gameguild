using System.ComponentModel.DataAnnotations;
using MediatR;

namespace GameGuild.Modules.Users.Queries;

/// <summary>
/// Query to get user by ID
/// </summary>
public class GetUserByIdQuery : IRequest<Models.User?>
{
    [Required]
    public Guid UserId { get; set; }

    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Query to get user by email
/// </summary>
public class GetUserByEmailQuery : IRequest<Models.User?>
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IncludeDeleted { get; set; } = false;
}

/// <summary>
/// Query to search users with filtering and pagination
/// </summary>
public class SearchUsersQuery : IRequest<IEnumerable<Models.User>>
{
    public string? SearchTerm { get; set; }
    
    public bool? IsActive { get; set; }
    
    public bool IncludeDeleted { get; set; } = false;
    
    public int Skip { get; set; } = 0;
    
    [Range(1, 100)]
    public int Take { get; set; } = 50;
    
    public decimal? MinBalance { get; set; }
    
    public decimal? MaxBalance { get; set; }
}
