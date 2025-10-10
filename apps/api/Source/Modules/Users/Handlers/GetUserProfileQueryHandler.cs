using GameGuild.CQRS;

namespace GameGuild.Modules.Users;

/// <summary> Handler for get user profile query using CQRS pattern </summary>
public class GetUserProfileQueryHandler(IUserRepository userRepository, ILogger<GetUserProfileQueryHandler> logger) : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly ILogger<GetUserProfileQueryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUserRepository _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing get user profile request for user {UserId}", request.UserId);

        try
        {
            // Get user information directly from repository
            var user = await _userRepository.GetByIdAsync(request.UserId, false, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", request.UserId);
                throw new InvalidOperationException($"User with ID {request.UserId} not found");
            }

            var userProfile = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Name = user.Name,
                GivenName = user.GivenName,
                FamilyName = user.FamilyName,
                DisplayName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : $"{user.GivenName} {user.FamilyName}".Trim(),
                PhoneNumber = user.PhoneNumber?.ToString(),
                IsActive = user.IsActive,
                Title = user.Title,
                Description = user.Description,
                IsEmailVerified = user.IsEmailVerified,
                LastSeenAt = user.LastSeenAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DeletedAt = user.DeletedAt,
                CurrentTenant = null, // TODO: Populate from tenant context
                AvailableTenants = new List<TenantInfo>() // TODO: Populate from user's tenant associations
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
