using GameGuild.Modules.Users;
using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Handler for get user profile query using CQRS pattern
/// </summary>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IUserService _userService;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUserService userService,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing get user profile request for user {UserId}", request.UserId);

        try
        {
            // Get user information
            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {request.UserId} not found");
            }

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Email, // Using email as username since User entity doesn't have Username
                GivenName = user.Name, // Using Name as GivenName
                FamilyName = "", // Not available in User entity
                DisplayName = user.Name,
                Title = "", // Not available in User entity
                Description = "", // Not available in User entity
                IsEmailVerified = true, // Assume verified for now
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CurrentTenant = null, // Would need separate service to get tenant info
                AvailableTenants = new List<TenantInfoDto>()
            };

            _logger.LogInformation("User profile retrieved successfully for user {UserId}", request.UserId);
            return userProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user profile for user {UserId}", request.UserId);
            throw;
        }
    }
}
