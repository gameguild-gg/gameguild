namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// Extension methods to convert from entity to DTO
/// </summary>
public static class ContentInteractionExtensions {
  public static ContentInteractionDto ToDto(this Models.ContentInteraction interaction) {
    return new ContentInteractionDto {
      Id = interaction.Id,
      ProgramUserId = interaction.ProgramUserId,
      ContentId = interaction.ContentId,
      Status = interaction.Status,
      SubmissionData = interaction.SubmissionData,
      CompletionPercentage = interaction.CompletionPercentage,
      TimeSpentMinutes = interaction.TimeSpentMinutes,
      FirstAccessedAt = interaction.FirstAccessedAt,
      LastAccessedAt = interaction.LastAccessedAt,
      CompletedAt = interaction.CompletedAt,
      SubmittedAt = interaction.SubmittedAt,
      CreatedAt = interaction.CreatedAt,
      UpdatedAt = interaction.UpdatedAt,
      Content =
        interaction.Content != null
          ? new ContentSummaryDto { Id = interaction.Content.Id, Title = interaction.Content.Title, ContentType = interaction.Content.Type.ToString(), EstimatedMinutes = interaction.Content.EstimatedMinutes }
          : null,
      ProgramUser = interaction.ProgramUser?.User != null ? new ProgramUserSummaryDto { Id = interaction.ProgramUser.User.Id, UserDisplayName = interaction.ProgramUser.User.Name, UserEmail = interaction.ProgramUser.User.Email } : null,
    };
  }

  public static IEnumerable<ContentInteractionDto> ToDto(this IEnumerable<Models.ContentInteraction> interactions) { return interactions.Select(i => i.ToDto()); }
}
