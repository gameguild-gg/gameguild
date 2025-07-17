using MediatR;


namespace GameGuild.Modules.UserProfiles;

/// <summary>
/// Notification handler that automatically creates a UserProfile when a user signs up
/// </summary>
public class UserSignedUpUserProfileHandler(
  IMediator mediator,
  ILogger<UserSignedUpUserProfileHandler> logger
) : INotificationHandler<Authentication.UserSignedUpNotification> {
  public async Task Handle(Authentication.UserSignedUpNotification notification, CancellationToken cancellationToken) {
    try {
      logger.LogInformation("Creating UserProfile for newly signed up user {UserId}", notification.UserId);

      // Extract names from username or email
      var (givenName, familyName) = ExtractNamesFromUserInfo(notification.Username, notification.Email);

      var createProfileCommand = new CreateUserProfileCommand {
        UserId = notification.UserId,
        GivenName = givenName,
        FamilyName = familyName,
        DisplayName = notification.Username,
        Title = "", // Default empty title
        Description = null, // No default description
        TenantId = notification.TenantId
      };

      var result = await mediator.Send(createProfileCommand, cancellationToken);

      if (result.IsSuccess) { logger.LogInformation("Successfully created UserProfile for user {UserId}", notification.UserId); }
      else {
        logger.LogWarning(
          "Failed to create UserProfile for user {UserId}: {ErrorMessage}",
          notification.UserId,
          result.ErrorMessage?.Description
        );
      }
    }
    catch (Exception ex) {
      logger.LogError(ex, "ErrorMessage creating UserProfile for user {UserId} during signup", notification.UserId);
      // Don't rethrow - we don't want to break the signup process if profile creation fails
    }
  }

  /// <summary>
  /// Extracts given name and family name from username or email
  /// </summary>
  private static (string GivenName, string FamilyName) ExtractNamesFromUserInfo(string username, string email) {
    // Try to extract from username first
    if (!string.IsNullOrEmpty(username) && username != email) {
      var names = username.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      if (names.Length >= 2) { return (names[0], string.Join(" ", names.Skip(1))); }

      // If username is a single word, use it as given name
      return (username, "");
    }

    // Fall back to email-based extraction
    if (!string.IsNullOrEmpty(email)) {
      var emailLocalPart = email.Split('@')[0];

      // Handle common email patterns like firstname.lastname
      if (emailLocalPart.Contains('.')) {
        var parts = emailLocalPart.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2) { return (CapitalizeFirst(parts[0]), CapitalizeFirst(string.Join(" ", parts.Skip(1)))); }
      }

      // Handle patterns like firstnamelastname or just use the email local part
      return (CapitalizeFirst(emailLocalPart), "");
    }

    // Fallback if nothing works
    return ("User", "");
  }

  /// <summary>
  /// Capitalizes the first letter of a string
  /// </summary>
  private static string CapitalizeFirst(string input) {
    if (string.IsNullOrEmpty(input)) return input;

    return char.ToUpper(input[0]) + input[1..].ToLower();
  }
}
