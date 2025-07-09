using GameGuild.Common.Domain.Enums;


namespace GameGuild.Modules.Programs.DTOs;

/// <summary>
/// DTO for ContentInteraction responses - avoids circular references for Swagger/OpenAPI
/// </summary>
public class ContentInteractionDto {
  public Guid Id { get; set; }

  public Guid ProgramUserId { get; set; }

  public Guid ContentId { get; set; }

  public ProgressStatus Status { get; set; }

  public string? SubmissionData { get; set; }

  public decimal CompletionPercentage { get; set; }

  public int? TimeSpentMinutes { get; set; }

  public DateTime? FirstAccessedAt { get; set; }

  public DateTime? LastAccessedAt { get; set; }

  public DateTime? CompletedAt { get; set; }

  public DateTime? SubmittedAt { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  // Simplified nested objects to avoid circular references
  public ContentSummaryDto? Content { get; set; }

  public ProgramUserSummaryDto? ProgramUser { get; set; }

  // Computed properties for convenience
  public bool IsSubmitted => SubmittedAt.HasValue;

  public bool IsCompleted => Status == ProgressStatus.Completed;

  public bool CanModify => !IsSubmitted;

  public int DurationInMinutes => TimeSpentMinutes ?? 0;
}

/// <summary>
/// Simplified content information to avoid circular references
/// </summary>
public class ContentSummaryDto {
  public Guid Id { get; set; }

  public string Title { get; set; } = string.Empty;

  public string ContentType { get; set; } = string.Empty;

  public int? EstimatedMinutes { get; set; }
}

/// <summary>
/// Simplified program user information to avoid circular references
/// </summary>
public class ProgramUserSummaryDto {
  public Guid Id { get; set; }

  public string UserDisplayName { get; set; } = string.Empty;

  public string UserEmail { get; set; } = string.Empty;
}

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
      ProgramUser = interaction.ProgramUser?.User != null ? new ProgramUserSummaryDto { Id = interaction.ProgramUser.User.Id, UserDisplayName = interaction.ProgramUser.User.Name, UserEmail = interaction.ProgramUser.User.Email } : null
    };
  }

  public static IEnumerable<ContentInteractionDto> ToDto(this IEnumerable<Models.ContentInteraction> interactions) { return interactions.Select(i => i.ToDto()); }
}
