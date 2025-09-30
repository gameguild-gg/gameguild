using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Handler for get user profile query using CQRS pattern </summary>
public class GetUserProfileQueryHandler(IUserService userService, ILogger<GetUserProfileQueryHandler> logger) : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly ILogger<GetUserProfileQueryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing get user profile request for user {UserId}", request.UserId);

        try
        {
            // Get user information
            var user = await _userService.GetUserByIdAsync(request.UserId);

            if (user == null) { throw new InvalidOperationException($"User with ID {request.UserId} not found"); }

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Email, // Using email as username since User entity doesn't have Username
                GivenName = user.GivenName, // Using Name as GivenName
                FamilyName = user.FamilyName, // Not available in User entity
                DisplayName = $"{user.GivenName} {user.FamilyName}".Trim(), // Combine GivenName and FamilyName
                Title = "", // Not available in User entity
                Description = "", // Not available in User entity
                IsEmailVerified = true, // Assume verified for now
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CurrentTenant = null, // Would need separate service to get tenant info
                AvailableTenants = [],
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
