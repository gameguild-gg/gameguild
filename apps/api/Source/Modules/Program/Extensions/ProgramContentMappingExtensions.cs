using GameGuild.Modules.Program.Models;
using GameGuild.Modules.Program.DTOs;
using System.Text.Json;

namespace GameGuild.Modules.Program.Extensions;

/// <summary>
/// Extension methods for mapping between ProgramContent entities and DTOs
/// </summary>
public static class ProgramContentMappingExtensions {
  /// <summary>
  /// Maps ProgramContent entity to DTO
  /// </summary>
  public static ProgramContentDto ToDto(this ProgramContent content) {
    return new ProgramContentDto {
      Id = content.Id,
      ProgramId = content.ProgramId,
      ParentId = content.ParentId,
      Title = content.Title,
      Description = content.Description,
      Type = content.Type,
      Body = string.IsNullOrEmpty(content.Body) ? null : JsonDocument.Parse(content.Body),
      SortOrder = content.SortOrder,
      IsRequired = content.IsRequired,
      GradingMethod = content.GradingMethod,
      MaxPoints = content.MaxPoints,
      EstimatedMinutes = content.EstimatedMinutes,
      Visibility = content.Visibility,
      CreatedAt = content.CreatedAt,
      UpdatedAt = content.UpdatedAt,
      ProgramTitle = content.Program?.Title,
      ParentTitle = content.Parent?.Title,
      ChildrenCount = content.Children?.Count(c => !c.IsDeleted) ?? 0,
      Children = content.Children?.Where(c => !c.IsDeleted).Select(c => c.ToDto()).ToList() ?? new List<ProgramContentDto>()
    };
  }

  /// <summary>
  /// Maps collection of ProgramContent entities to DTOs
  /// </summary>
  public static IEnumerable<ProgramContentDto> ToDtos(this IEnumerable<ProgramContent> contents) { return contents.Select(c => c.ToDto()); }

  /// <summary>
  /// Maps CreateProgramContentDto to ProgramContent entity
  /// </summary>
  public static ProgramContent ToEntity(this CreateProgramContentDto dto) {
    return new ProgramContent {
      Id = Guid.NewGuid(),
      ProgramId = dto.ProgramId,
      ParentId = dto.ParentId,
      Title = dto.Title,
      Description = dto.Description,
      Type = dto.Type,
      Body = dto.Body,
      SortOrder = dto.SortOrder,
      IsRequired = dto.IsRequired,
      GradingMethod = dto.GradingMethod,
      MaxPoints = dto.MaxPoints,
      EstimatedMinutes = dto.EstimatedMinutes,
      Visibility = dto.Visibility
    };
  }

  /// <summary>
  /// Applies updates from UpdateProgramContentDto to ProgramContent entity
  /// </summary>
  public static void ApplyUpdates(this ProgramContent content, UpdateProgramContentDto dto) {
    if (dto.Title != null) content.Title = dto.Title;
    if (dto.Description != null) content.Description = dto.Description;
    if (dto.Type != null) content.Type = dto.Type.Value;
    if (dto.Body != null) content.Body = dto.Body;
    if (dto.SortOrder != null) content.SortOrder = dto.SortOrder.Value;
    if (dto.IsRequired != null) content.IsRequired = dto.IsRequired.Value;
    if (dto.GradingMethod != null) content.GradingMethod = dto.GradingMethod;
    if (dto.MaxPoints != null) content.MaxPoints = dto.MaxPoints;
    if (dto.EstimatedMinutes != null) content.EstimatedMinutes = dto.EstimatedMinutes;
    if (dto.Visibility != null) content.Visibility = dto.Visibility.Value;
  }
}
