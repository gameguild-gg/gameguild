using System.Text.Json;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Extension methods for converting entities to DTOs
/// </summary>
public static class RecommendationDtoExtensions
{
    /// <summary>
    /// Convert CourseRecommendation entity to RecommendationDto
    /// </summary>
    public static RecommendationDto ToDto(this CourseRecommendation entity) => new(
        Id: entity.Id,
        UserId: entity.UserId,
        CourseId: entity.CourseId,
        Type: entity.Type,
        Score: entity.Score,
        Reason: entity.Reason,
        IsViewed: entity.IsViewed,
        IsDismissed: entity.IsDismissed,
        ExpiresAt: entity.ExpiresAt,
        CreatedAt: entity.CreatedAt);

    /// <summary>
    /// Convert UserLearningProfile entity to UserLearningProfileDto
    /// </summary>
    public static UserLearningProfileDto ToDto(this UserLearningProfile entity) => new(
        Id: entity.Id,
        UserId: entity.UserId,
        PreferredCategories: ParseJsonArray(entity.PreferredCategories),
        PreferredDifficulty: entity.PreferredDifficulty,
        PreferredDuration: entity.PreferredDuration,
        LearningGoals: ParseJsonArray(entity.LearningGoals),
        Skills: ParseJsonArray(entity.Skills),
        TotalCoursesCompleted: entity.TotalCoursesCompleted,
        TotalHoursLearned: entity.TotalHoursLearned,
        LastActivityAt: entity.LastActivityAt,
        CreatedAt: entity.CreatedAt,
        UpdatedAt: entity.UpdatedAt);

    /// <summary>
    /// Update UserLearningProfile from DTO
    /// </summary>
    public static void UpdateFromDto(this UserLearningProfile entity, CreateOrUpdateLearningProfileDto dto)
    {
        // Update via reflection or create domain methods in entity
        // For now, we'll use the UpdatePreferences method
        entity.UpdatePreferences(
            preferredCategories: SerializeJsonArray(dto.PreferredCategories),
            preferredDifficulty: dto.PreferredDifficulty,
            preferredDuration: dto.PreferredDuration,
            learningGoals: SerializeJsonArray(dto.LearningGoals),
            skills: SerializeJsonArray(dto.Skills));
    }

    private static string[]? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<string[]>(json);
        }
        catch
        {
            return null;
        }
    }

    private static string? SerializeJsonArray(string[]? array)
    {
        if (array == null || array.Length == 0) return null;
        return JsonSerializer.Serialize(array);
    }
}
