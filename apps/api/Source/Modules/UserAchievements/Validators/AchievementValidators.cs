using FluentValidation;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Validator for CreateAchievementCommand
/// </summary>
public class CreateAchievementCommandValidator : AbstractValidator<CreateAchievementCommand> {
  public CreateAchievementCommandValidator() {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Achievement name is required")
      .MaximumLength(100)
      .WithMessage("Achievement name cannot exceed 100 characters");

    RuleFor(x => x.Description)
      .MaximumLength(500)
      .WithMessage("Achievement description cannot exceed 500 characters");

    RuleFor(x => x.Category)
      .NotEmpty()
      .WithMessage("Achievement category is required")
      .MaximumLength(50)
      .WithMessage("Achievement category cannot exceed 50 characters");

    RuleFor(x => x.Type)
      .NotEmpty()
      .WithMessage("Achievement type is required")
      .MaximumLength(50)
      .WithMessage("Achievement type cannot exceed 50 characters");

    RuleFor(x => x.IconUrl)
      .MaximumLength(255)
      .WithMessage("Icon URL cannot exceed 255 characters")
      .Must(BeValidUrlOrNull)
      .WithMessage("Icon URL must be a valid URL");

    RuleFor(x => x.Color)
      .MaximumLength(7)
      .WithMessage("Color must be a valid hex color code")
      .Must(BeValidHexColorOrNull)
      .WithMessage("Color must be a valid hex color code (e.g., #FF0000)");

    RuleFor(x => x.Points)
      .GreaterThanOrEqualTo(0)
      .WithMessage("Points must be greater than or equal to 0");

    RuleFor(x => x.DisplayOrder)
      .GreaterThanOrEqualTo(0)
      .WithMessage("Display order must be greater than or equal to 0");

    RuleFor(x => x.Levels)
      .Must(BeValidLevelsOrNull)
      .WithMessage("Achievement levels must have unique level numbers and valid data");
  }

  private static bool BeValidUrlOrNull(string? url) {
    if (string.IsNullOrEmpty(url)) return true;
    return Uri.TryCreate(url, UriKind.Absolute, out _);
  }

  private static bool BeValidHexColorOrNull(string? color) {
    if (string.IsNullOrEmpty(color)) return true;
    return color.StartsWith("#") && color.Length == 7 && 
           color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
  }

  private static bool BeValidLevelsOrNull(List<CreateAchievementLevelCommand>? levels) {
    if (levels == null || !levels.Any()) return true;
    
    // Check for unique level numbers
    var levelNumbers = levels.Select(l => l.Level).ToList();
    if (levelNumbers.Count != levelNumbers.Distinct().Count()) return false;

    // Check for valid data
    return levels.All(l => l.Level > 0 && 
                          !string.IsNullOrEmpty(l.Name) && 
                          l.RequiredProgress >= 0 && 
                          l.Points >= 0);
  }
}

/// <summary>
/// Validator for UpdateAchievementCommand
/// </summary>
public class UpdateAchievementCommandValidator : AbstractValidator<UpdateAchievementCommand> {
  public UpdateAchievementCommandValidator() {
    RuleFor(x => x.AchievementId)
      .NotEmpty()
      .WithMessage("Achievement ID is required");

    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");

    RuleFor(x => x.Name)
      .MaximumLength(100)
      .WithMessage("Achievement name cannot exceed 100 characters")
      .When(x => !string.IsNullOrEmpty(x.Name));

    RuleFor(x => x.Description)
      .MaximumLength(500)
      .WithMessage("Achievement description cannot exceed 500 characters");

    RuleFor(x => x.Category)
      .MaximumLength(50)
      .WithMessage("Achievement category cannot exceed 50 characters")
      .When(x => !string.IsNullOrEmpty(x.Category));

    RuleFor(x => x.Type)
      .MaximumLength(50)
      .WithMessage("Achievement type cannot exceed 50 characters")
      .When(x => !string.IsNullOrEmpty(x.Type));

    RuleFor(x => x.IconUrl)
      .MaximumLength(255)
      .WithMessage("Icon URL cannot exceed 255 characters")
      .Must(BeValidUrlOrNull)
      .WithMessage("Icon URL must be a valid URL");

    RuleFor(x => x.Color)
      .MaximumLength(7)
      .WithMessage("Color must be a valid hex color code")
      .Must(BeValidHexColorOrNull)
      .WithMessage("Color must be a valid hex color code (e.g., #FF0000)");

    RuleFor(x => x.Points)
      .GreaterThanOrEqualTo(0)
      .WithMessage("Points must be greater than or equal to 0")
      .When(x => x.Points.HasValue);

    RuleFor(x => x.DisplayOrder)
      .GreaterThanOrEqualTo(0)
      .WithMessage("Display order must be greater than or equal to 0")
      .When(x => x.DisplayOrder.HasValue);
  }

  private static bool BeValidUrlOrNull(string? url) {
    if (string.IsNullOrEmpty(url)) return true;
    return Uri.TryCreate(url, UriKind.Absolute, out _);
  }

  private static bool BeValidHexColorOrNull(string? color) {
    if (string.IsNullOrEmpty(color)) return true;
    return color.StartsWith("#") && color.Length == 7 && 
           color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
  }
}

/// <summary>
/// Validator for AwardAchievementCommand
/// </summary>
public class AwardAchievementCommandValidator : AbstractValidator<AwardAchievementCommand> {
  public AwardAchievementCommandValidator() {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");

    RuleFor(x => x.AchievementId)
      .NotEmpty()
      .WithMessage("Achievement ID is required");

    RuleFor(x => x.Progress)
      .GreaterThanOrEqualTo(0)
      .WithMessage("Progress must be greater than or equal to 0");

    RuleFor(x => x.MaxProgress)
      .GreaterThan(0)
      .WithMessage("Max progress must be greater than 0");

    RuleFor(x => x.Progress)
      .LessThanOrEqualTo(x => x.MaxProgress)
      .WithMessage("Progress cannot exceed max progress");

    RuleFor(x => x.Level)
      .GreaterThan(0)
      .WithMessage("Level must be greater than 0")
      .When(x => x.Level.HasValue);
  }
}

/// <summary>
/// Validator for UpdateAchievementProgressCommand
/// </summary>
public class UpdateAchievementProgressCommandValidator : AbstractValidator<UpdateAchievementProgressCommand> {
  public UpdateAchievementProgressCommandValidator() {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");

    RuleFor(x => x.AchievementId)
      .NotEmpty()
      .WithMessage("Achievement ID is required");

    RuleFor(x => x.ProgressIncrement)
      .GreaterThan(0)
      .WithMessage("Progress increment must be greater than 0");
  }
}

/// <summary>
/// Validator for RevokeAchievementCommand
/// </summary>
public class RevokeAchievementCommandValidator : AbstractValidator<RevokeAchievementCommand> {
  public RevokeAchievementCommandValidator() {
    RuleFor(x => x.UserAchievementId)
      .NotEmpty()
      .WithMessage("User achievement ID is required");

    RuleFor(x => x.RevokedByUserId)
      .NotEmpty()
      .WithMessage("Revoked by user ID is required");

    RuleFor(x => x.Reason)
      .MaximumLength(500)
      .WithMessage("Reason cannot exceed 500 characters");
  }
}

/// <summary>
/// Validator for BulkAwardAchievementCommand
/// </summary>
public class BulkAwardAchievementCommandValidator : AbstractValidator<BulkAwardAchievementCommand> {
  public BulkAwardAchievementCommandValidator() {
    RuleFor(x => x.AchievementId)
      .NotEmpty()
      .WithMessage("Achievement ID is required");

    RuleFor(x => x.AwardedByUserId)
      .NotEmpty()
      .WithMessage("Awarded by user ID is required");

    RuleFor(x => x.UserIds)
      .Must(BeValidUserListOrNull)
      .WithMessage("User IDs list must not be empty if provided");

    RuleFor(x => x)
      .Must(HaveEitherUserIdsOrCriteria)
      .WithMessage("Either UserIds or UserCriteria must be provided");
  }

  private static bool BeValidUserListOrNull(List<Guid>? userIds) {
    return userIds == null || userIds.Any();
  }

  private static bool HaveEitherUserIdsOrCriteria(BulkAwardAchievementCommand command) {
    return (command.UserIds?.Any() == true) || !string.IsNullOrEmpty(command.UserCriteria);
  }
}

/// <summary>
/// Validator for GetAchievementsQuery
/// </summary>
public class GetAchievementsQueryValidator : AbstractValidator<GetAchievementsQuery> {
  public GetAchievementsQueryValidator() {
    RuleFor(x => x.PageNumber)
      .GreaterThan(0)
      .WithMessage("Page number must be greater than 0");

    RuleFor(x => x.PageSize)
      .GreaterThan(0)
      .LessThanOrEqualTo(100)
      .WithMessage("Page size must be between 1 and 100");

    RuleFor(x => x.OrderBy)
      .Must(BeValidOrderBy)
      .WithMessage("Order by must be one of: DisplayOrder, Name, Points, CreatedAt");
  }

  private static bool BeValidOrderBy(string orderBy) {
    var validOrderBy = new[] { "DisplayOrder", "Name", "Points", "CreatedAt" };
    return validOrderBy.Contains(orderBy, StringComparer.OrdinalIgnoreCase);
  }
}

/// <summary>
/// Validator for GetUserAchievementsQuery
/// </summary>
public class GetUserAchievementsQueryValidator : AbstractValidator<GetUserAchievementsQuery> {
  public GetUserAchievementsQueryValidator() {
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("User ID is required");

    RuleFor(x => x.PageNumber)
      .GreaterThan(0)
      .WithMessage("Page number must be greater than 0");

    RuleFor(x => x.PageSize)
      .GreaterThan(0)
      .LessThanOrEqualTo(100)
      .WithMessage("Page size must be between 1 and 100");

    RuleFor(x => x.OrderBy)
      .Must(BeValidOrderBy)
      .WithMessage("Order by must be one of: EarnedAt, Points, AchievementName");
  }

  private static bool BeValidOrderBy(string orderBy) {
    var validOrderBy = new[] { "EarnedAt", "Points", "AchievementName" };
    return validOrderBy.Contains(orderBy, StringComparer.OrdinalIgnoreCase);
  }
}
