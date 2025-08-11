namespace GameGuild.Modules.Programs;

// Program Management DTOs
public record CreateProgramDto(string Title, string? Description, string Slug, string? Thumbnail = null) {
  public string Title { get; init; } = Title;

  public string? Description { get; init; } = Description;

  public string Slug { get; init; } = Slug;

  public string? Thumbnail { get; init; } = Thumbnail;
}

// Content Management DTOs

// Workflow DTOs

// User Progress DTOs

// Scheduling DTOs

// Monetization DTOs

// Product Integration DTOs

// Analytics DTOs

// Search and Filter DTOs

// Bulk Operations DTOs
