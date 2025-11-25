namespace GameGuild.Modules.Programs.DTOs;

public record ProgramUserProgress(Guid ProgramId, int CompletedCount, int TotalCount, decimal ProgressPercentage);