namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// GraphQL type for Achievement entity
/// </summary>
public class AchievementType : ObjectType<Achievement> {
  protected override void Configure(IObjectTypeDescriptor<Achievement> descriptor) {
    descriptor.Name("Achievement");
    descriptor.Description("Represents a gamification achievement that users can earn");

    descriptor
      .Field(a => a.Id)
      .Description("The unique identifier of the achievement");

    descriptor
      .Field(a => a.Name)
      .Description("The name of the achievement");

    descriptor
      .Field(a => a.Description)
      .Description("The description of what the achievement represents");

    descriptor
      .Field(a => a.Category)
      .Description("The category this achievement belongs to");

    descriptor
      .Field(a => a.Type)
      .Description("The type of achievement (badge, trophy, milestone, etc.)");

    descriptor
      .Field(a => a.IconUrl)
      .Description("URL to the achievement icon/image");

    descriptor
      .Field(a => a.Color)
      .Description("Color associated with the achievement");

    descriptor
      .Field(a => a.Points)
      .Description("Points awarded for earning this achievement");

    descriptor
      .Field(a => a.IsActive)
      .Description("Whether the achievement is active and can be earned");

    descriptor
      .Field(a => a.IsSecret)
      .Description("Whether this is a secret achievement");

    descriptor
      .Field(a => a.IsRepeatable)
      .Description("Whether this achievement can be earned multiple times");

    descriptor
      .Field(a => a.Conditions)
      .Description("Conditions required to earn this achievement");

    descriptor
      .Field(a => a.DisplayOrder)
      .Description("Display order for sorting achievements");

    descriptor
      .Field(a => a.CreatedAt)
      .Description("When the achievement was created");

    descriptor
      .Field(a => a.UpdatedAt)
      .Description("When the achievement was last updated");

    descriptor
      .Field(a => a.Levels)
      .Description("Achievement levels if this is a multi-level achievement")
      .ResolveWith<AchievementResolvers>(r => r.GetLevelsAsync(default!, default!, default!));

    descriptor
      .Field(a => a.Prerequisites)
      .Description("Prerequisites required before this achievement can be earned")
      .ResolveWith<AchievementResolvers>(r => r.GetPrerequisitesAsync(default!, default!, default!));

    descriptor
      .Field("userAchievements")
      .Description("Users who have earned this achievement")
      .Type<ListType<UserAchievementType>>()
      .ResolveWith<AchievementResolvers>(r => r.GetUserAchievementsAsync(default!, default!, default!, default!));

    descriptor
      .Field("statistics")
      .Description("Statistics about this achievement")
      .Type<AchievementStatisticsType>()
      .ResolveWith<AchievementResolvers>(r => r.GetStatisticsAsync(default!, default!, default!));

    descriptor
      .Field("earnCount")
      .Description("Total number of times this achievement has been earned")
      .Type<IntType>()
      .ResolveWith<AchievementResolvers>(r => r.GetEarnCountAsync(default!, default!, default!));
  }
}

/// <summary>
/// GraphQL type for UserAchievement entity
/// </summary>
public class UserAchievementType : ObjectType<UserAchievement> {
  protected override void Configure(IObjectTypeDescriptor<UserAchievement> descriptor) {
    descriptor.Name("UserAchievement");
    descriptor.Description("Represents a user's earned achievement");

    descriptor
      .Field(ua => ua.Id)
      .Description("The unique identifier of the user achievement");

    descriptor
      .Field(ua => ua.UserId)
      .Description("The ID of the user who earned the achievement");

    descriptor
      .Field(ua => ua.AchievementId)
      .Description("The ID of the achievement that was earned");

    descriptor
      .Field(ua => ua.Achievement)
      .Description("The achievement that was earned")
      .ResolveWith<UserAchievementResolvers>(r => r.GetAchievementAsync(default!, default!, default!));

    descriptor
      .Field(ua => ua.User)
      .Description("The user who earned the achievement")
      .ResolveWith<UserAchievementResolvers>(r => r.GetUserAsync(default!, default!, default!));

    descriptor
      .Field(ua => ua.EarnedAt)
      .Description("When the achievement was earned");

    descriptor
      .Field(ua => ua.Level)
      .Description("The level achieved if this is a multi-level achievement");

    descriptor
      .Field(ua => ua.Progress)
      .Description("Current progress towards this achievement");

    descriptor
      .Field(ua => ua.MaxProgress)
      .Description("Maximum progress required for completion");

    descriptor
      .Field(ua => ua.IsCompleted)
      .Description("Whether the achievement has been completed");

    descriptor
      .Field(ua => ua.IsNotified)
      .Description("Whether the user has been notified about earning this achievement");

    descriptor
      .Field(ua => ua.Context)
      .Description("Additional context about how the achievement was earned");

    descriptor
      .Field(ua => ua.PointsEarned)
      .Description("Points earned from this achievement");

    descriptor
      .Field(ua => ua.EarnCount)
      .Description("Number of times this achievement has been earned (for repeatable achievements)");

    descriptor
      .Field("progressPercentage")
      .Description("Progress as a percentage")
      .Type<FloatType>()
      .ResolveWith<UserAchievementResolvers>(r => r.GetProgressPercentageAsync(default!));
  }
}

/// <summary>
/// GraphQL type for AchievementLevel entity
/// </summary>
public class AchievementLevelType : ObjectType<AchievementLevel> {
  protected override void Configure(IObjectTypeDescriptor<AchievementLevel> descriptor) {
    descriptor.Name("AchievementLevel");
    descriptor.Description("Represents a level within a multi-level achievement");

    descriptor
      .Field(al => al.Id)
      .Description("The unique identifier of the achievement level");

    descriptor
      .Field(al => al.Level)
      .Description("The level number");

    descriptor
      .Field(al => al.Name)
      .Description("The name of this level");

    descriptor
      .Field(al => al.Description)
      .Description("Description of what this level represents");

    descriptor
      .Field(al => al.RequiredProgress)
      .Description("Progress required to reach this level");

    descriptor
      .Field(al => al.Points)
      .Description("Points awarded for reaching this level");

    descriptor
      .Field(al => al.IconUrl)
      .Description("Icon specific to this level");

    descriptor
      .Field(al => al.Color)
      .Description("Color specific to this level");

    descriptor
      .Field(al => al.Achievement)
      .Description("The achievement this level belongs to")
      .ResolveWith<AchievementLevelResolvers>(r => r.GetAchievementAsync(default!, default!, default!));
  }
}

/// <summary>
/// GraphQL type for AchievementProgress entity
/// </summary>
public class AchievementProgressType : ObjectType<AchievementProgress> {
  protected override void Configure(IObjectTypeDescriptor<AchievementProgress> descriptor) {
    descriptor.Name("AchievementProgress");
    descriptor.Description("Represents a user's progress towards an achievement");

    descriptor
      .Field(ap => ap.Id)
      .Description("The unique identifier of the achievement progress");

    descriptor
      .Field(ap => ap.UserId)
      .Description("The ID of the user making progress");

    descriptor
      .Field(ap => ap.AchievementId)
      .Description("The ID of the achievement being progressed towards");

    descriptor
      .Field(ap => ap.Achievement)
      .Description("The achievement being progressed towards")
      .ResolveWith<AchievementProgressResolvers>(r => r.GetAchievementAsync(default!, default!, default!));

    descriptor
      .Field(ap => ap.User)
      .Description("The user making progress")
      .ResolveWith<AchievementProgressResolvers>(r => r.GetUserAsync(default!, default!, default!));

    descriptor
      .Field(ap => ap.CurrentProgress)
      .Description("Current progress value");

    descriptor
      .Field(ap => ap.TargetProgress)
      .Description("Target progress required for completion");

    descriptor
      .Field("progressPercentage")
      .Description("Progress as a percentage")
      .Type<FloatType>()
      .ResolveWith<AchievementProgressResolvers>(r => r.GetProgressPercentageAsync(default!));

    descriptor
      .Field(ap => ap.LastUpdated)
      .Description("When progress was last updated");

    descriptor
      .Field(ap => ap.IsCompleted)
      .Description("Whether this achievement has been completed");

    descriptor
      .Field(ap => ap.Context)
      .Description("Additional context data");
  }
}
