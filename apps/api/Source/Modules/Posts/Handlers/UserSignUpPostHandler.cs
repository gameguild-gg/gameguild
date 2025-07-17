using MediatR;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Handler that creates a welcome post when a new user signs up
/// Integrates with the existing Content/Resource architecture
/// </summary>
public class UserSignUpPostHandler(
  IMediator mediator,
  ILogger<UserSignUpPostHandler> logger
) : INotificationHandler<UserSignedUpNotification> {
  public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken) {
    try {
      logger.LogInformation(
        "Creating welcome post for new user {UserId} ({Email})",
        notification.UserId,
        notification.Email
      );

      // Generate welcoming content and metadata
      var (title, description) = GenerateWelcomeContent(notification);
      var richContent = GenerateRichWelcomeContent(notification);

      var createPostCommand = new CreatePostCommand {
        Title = title,
        Description = description,
        PostType = "user_signup",
        AuthorId = notification.UserId,
        IsSystemGenerated = true,
        Visibility = AccessLevel.Public, // Welcome posts are public
        RichContent = richContent,
        TenantId = notification.TenantId
      };

      var result = await mediator.Send(createPostCommand, cancellationToken);

      if (result.IsSuccess) {
        logger.LogInformation(
          "Successfully created welcome post {PostId} for user {UserId}",
          result.Value?.Id,
          notification.UserId
        );
      }
      else {
        logger.LogWarning(
          "Failed to create welcome post for user {UserId}: {Error}",
          notification.UserId,
          result.Error?.Description
        );
      }
    }
    catch (Exception ex) {
      logger.LogError(ex, "Error creating welcome post for user {UserId} during signup", notification.UserId);
      // Don't rethrow - we don't want to break the signup process if post creation fails
    }
  }

  /// <summary>
  /// Generates personalized welcome content for the new user
  /// </summary>
  private static (string Title, string Description) GenerateWelcomeContent(UserSignedUpNotification notification) {
    var displayName = notification.Username ?? GetDisplayNameFromEmail(notification.Email);
    var signUpMethod = DetermineSignUpMethod(notification);

    var welcomeTitles = new[] { $"🎉 Welcome {displayName} to Game Guild!", $"👋 {displayName} joined the community!", $"� New member alert: {displayName}!", $"⭐ Say hello to {displayName}!" };

    var welcomeDescriptions = new[] {
      $"We're excited to have {displayName} join our Game Guild community! Let's give them a warm welcome.",
      $"Everyone please welcome {displayName} to our growing community of creators and learners.",
      $"{displayName} just started their Game Guild journey. Welcome aboard!",
      $"The Game Guild family grows with {displayName}! Welcome to the community."
    };

    var random = new Random();
    var title = welcomeTitles[random.Next(welcomeTitles.Length)];
    var description = welcomeDescriptions[random.Next(welcomeDescriptions.Length)];

    if (signUpMethod == "Google") { description += " 🔗 Signed up with Google."; }

    return (title, description);
  }

  /// <summary>
  /// Generates rich content for the welcome post
  /// </summary>
  private static string GenerateRichWelcomeContent(UserSignedUpNotification notification) {
    var displayName = notification.Username ?? GetDisplayNameFromEmail(notification.Email);
    var signUpMethod = DetermineSignUpMethod(notification);

    var richContent = new {
      type = "welcome_post",
      user = new {
        id = notification.UserId,
        displayName = displayName,
        email = notification.Email,
        signUpMethod = signUpMethod,
        signUpTime = notification.SignUpTime
      },
      welcome = new {
        message = $"Welcome to Game Guild, {displayName}! 🎮",
        tips = new[] { "Explore our community programs and projects", "Connect with other creators and learners", "Share your own projects and achievements", "Participate in community challenges and events" }
      },
      metadata = new { auto_generated = true, post_type = "user_signup", template_version = "1.0" }
    };

    return System.Text.Json.JsonSerializer.Serialize(richContent, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
  }

  /// <summary>
  /// Determines the signup method based on available information
  /// </summary>
  private static string DetermineSignUpMethod(UserSignedUpNotification notification) {
    // This is a heuristic - in a real implementation, you might want to add 
    // a SignUpMethod property to the notification
    if (notification.Username == notification.Email) return "Google"; // Google OAuth typically sets username to email

    return "Local";
  }

  /// <summary>
  /// Extracts a display name from email address
  /// </summary>
  private static string GetDisplayNameFromEmail(string email) {
    if (string.IsNullOrWhiteSpace(email)) return "New Member";

    var localPart = email.Split('@')[0];

    // Convert common patterns like "john.doe" or "john_doe" to "John Doe"
    var name = localPart.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');

    // Capitalize each word
    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
  }
}
