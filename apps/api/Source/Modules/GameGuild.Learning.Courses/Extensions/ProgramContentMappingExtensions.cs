

using System.Text.Json;


namespace GameGuild.Learning.Courses;

/// <summary> Extension methods for mapping between ProgramContent entities and DTOs </summary>
public static class ProgramContentMappingExtensions
{
  public static ProgramContentType NormalizeProfessorFacingType(ProgramContentType type) =>
    type switch
    {
      ProgramContentType.Page => ProgramContentType.Lesson,
      ProgramContentType.Challenge => ProgramContentType.Assignment,
      _ => type,
    };

  /// <summary> Maps ProgramContent entity to DTO </summary>
  public static ProgramContentDto ToDto(this ProgramContent content)
  {
    var normalizedType = NormalizeProfessorFacingType(content.Type);
    var isLesson = normalizedType == ProgramContentType.Lesson;

    return new ProgramContentDto
    {
      Id = content.Id,
      ProgramId = content.ProgramId,
      ParentId = content.ParentId,
      Title = content.Title,
      Description = content.Description ?? string.Empty,
      Type = normalizedType,
      Body = ParseBody(content.Body),
      LessonFormat = isLesson ? content.LessonFormat ?? LessonContentFormatInference.FromBody(content.Body) : null,
      SortOrder = content.SortOrder,
      IsRequired = content.IsRequired,
      GradingMethod = isLesson ? GradingMethod.None : content.GradingMethod,
      MaxPoints = isLesson ? null : content.MaxPoints,
      EstimatedMinutes = content.EstimatedMinutes,
      Visibility = content.Visibility,
      CreatedAt = content.CreatedAt,
      UpdatedAt = content.UpdatedAt,
      ProgramTitle = content.Program?.Title,
      ParentTitle = content.Parent?.Title,
      ChildrenCount = content.Children?.Count(c => c.DeletedAt == null) ?? 0,
      Children = content.Children?.Where(c => c.DeletedAt == null).Select(c => c.ToDto()).ToList() ?? new List<ProgramContentDto>(),
    };
  }

  /// <summary> Maps collection of ProgramContent entities to DTOs </summary>
  public static IEnumerable<ProgramContentDto> ToDtos(this IEnumerable<ProgramContent> contents) { return contents.Select(c => c.ToDto()); }

  private static JsonDocument? ParseBody(string? body)
  {
    if (string.IsNullOrWhiteSpace(body)) return null;

    var normalizedBody = StripImportMarker(body);

    try
    {
      return JsonDocument.Parse(normalizedBody);
    }
    catch (JsonException)
    {
      return JsonDocument.Parse(JsonSerializer.Serialize(new { markdown = normalizedBody }));
    }
  }

  private static string StripImportMarker(string body)
  {
    const string importMarkerPrefix = "<!-- gameguild-source:";

    if (!body.StartsWith(importMarkerPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return body;
    }

    var markerEnd = body.IndexOf("-->", StringComparison.Ordinal);
    if (markerEnd < 0)
    {
      return body;
    }

    var contentStart = markerEnd + 3;
    while (contentStart < body.Length)
    {
      var current = body[contentStart];
      if (current != '\r' && current != '\n')
      {
        break;
      }

      contentStart++;
    }

    return body[contentStart..];
  }

  /// <summary> Maps CreateProgramContentDto to ProgramContent entity </summary>
  public static ProgramContent ToEntity(this CreateProgramContentDto dto)
  {
    var content = new ProgramContent
    {
      Id = Guid.NewGuid(),
      ProgramId = dto.ProgramId,
      ParentId = dto.ParentId,
      Title = dto.Title,
      Description = dto.Description,
      Type = NormalizeProfessorFacingType(dto.Type),
      Body = dto.Body,
      LessonFormat = dto.LessonFormat ?? LessonContentFormatInference.FromBody(dto.Body),
      SortOrder = dto.SortOrder,
      IsRequired = dto.IsRequired,
      GradingMethod = dto.GradingMethod ?? GradingMethod.None,
      MaxPoints = (int?)dto.MaxPoints,
      EstimatedMinutes = dto.EstimatedMinutes,
      Visibility = dto.Visibility,
    };

    content.NormalizeLearningContract();
    return content;
  }

  /// <summary> Applies updates from UpdateProgramContentDto to ProgramContent entity </summary>
  public static void ApplyUpdates(this ProgramContent content, UpdateProgramContentDto dto)
  {
    var wasLesson = NormalizeProfessorFacingType(content.Type) == ProgramContentType.Lesson;
    if (dto.Title != null) content.Title = dto.Title;
    if (dto.Description != null) content.Description = dto.Description;
    if (dto.Type != null) content.Type = NormalizeProfessorFacingType(dto.Type.Value);
    if (dto.Body != null) content.Body = dto.Body;
    if (dto.LessonFormat.HasValue)
    {
      content.LessonFormat = dto.LessonFormat.Value;
    }
    else if (NormalizeProfessorFacingType(content.Type) == ProgramContentType.Lesson &&
             (!wasLesson || !content.LessonFormat.HasValue))
    {
      content.LessonFormat = LessonContentFormatInference.FromBody(content.Body);
    }
    if (dto.SortOrder != null) content.SortOrder = dto.SortOrder.Value;
    if (dto.IsRequired != null) content.IsRequired = dto.IsRequired.Value;
    if (dto.GradingMethod != null) content.GradingMethod = dto.GradingMethod.Value;
    if (dto.MaxPoints.HasValue) content.MaxPoints = (int?)dto.MaxPoints.Value;
    if (dto.EstimatedMinutes != null) content.EstimatedMinutes = dto.EstimatedMinutes;
    if (dto.Visibility != null) content.Visibility = dto.Visibility.Value;
    content.NormalizeLearningContract();
  }
}
