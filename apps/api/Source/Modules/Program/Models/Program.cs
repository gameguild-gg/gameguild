using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Modules.Feedback.Models;
using GameGuild.Modules.Product.Models;
using GameGuild.Common.Enums;


namespace GameGuild.Modules.Program.Models;

[Table("programs")]
[Index(nameof(Visibility))]
[Index(nameof(Status))]
[Index(nameof(Slug))]
[Index(nameof(Category))]
[Index(nameof(Difficulty))]
public class Program : Content {
  private string? _thumbnail;

  private ICollection<ProgramContent> _programContents = new List<ProgramContent>();

  private ICollection<ProgramUser> _programUsers = new List<ProgramUser>();

  private ICollection<ProductProgram> _productPrograms = new List<Product.Models.ProductProgram>();

  private ICollection<Certificate.Models.Certificate> _certificates = new List<Certificate.Models.Certificate>();

  private ICollection<ProgramFeedbackSubmission> _feedbackSubmissions =
    new List<Feedback.Models.ProgramFeedbackSubmission>();

  private ICollection<ProgramRating> _programRatings = new List<Feedback.Models.ProgramRating>();

  [MaxLength(500)]
  public string? Thumbnail {
    get => _thumbnail;
    set => _thumbnail = value;
  }

  /// <summary>
  /// Category of the program (Programming, DataScience, etc.)
  /// </summary>
  public ProgramCategory Category { get; set; } = ProgramCategory.Other;

  /// <summary>
  /// Difficulty level of the program
  /// </summary>
  public ProgramDifficulty Difficulty { get; set; } = ProgramDifficulty.Beginner;

  // Navigation properties
  public virtual ICollection<ProgramContent> ProgramContents {
    get => _programContents;
    set => _programContents = value;
  }

  public virtual ICollection<ProgramUser> ProgramUsers {
    get => _programUsers;
    set => _programUsers = value;
  }

  public virtual ICollection<Product.Models.ProductProgram> ProductPrograms {
    get => _productPrograms;
    set => _productPrograms = value;
  }

  public virtual ICollection<Certificate.Models.Certificate> Certificates {
    get => _certificates;
    set => _certificates = value;
  }

  public virtual ICollection<Feedback.Models.ProgramFeedbackSubmission> FeedbackSubmissions {
    get => _feedbackSubmissions;
    set => _feedbackSubmissions = value;
  }

  public virtual ICollection<Feedback.Models.ProgramRating> ProgramRatings {
    get => _programRatings;
    set => _programRatings = value;
  }

  // Helper methods for JSON metadata
  public T? GetMetadata<T>(string key) where T : class {
    if (Metadata?.AdditionalData == null) return null;

    try {
      var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData);

      if (metadataDict != null && metadataDict.TryGetValue(key, out var value)) { return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value)); }
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  public void SetMetadata<T>(string key, T value) {
    if (Metadata == null) { Metadata = new ResourceMetadata { ResourceType = nameof(Program), AdditionalData = "{}" }; }

    var metadataDict = string.IsNullOrEmpty(Metadata.AdditionalData)
                         ? new Dictionary<string, object>()
                         : JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData) ??
                           new Dictionary<string, object>();

    metadataDict[key] = value!;
    Metadata.AdditionalData = JsonSerializer.Serialize(metadataDict);
  }
}

/// <summary>
/// Entity Framework configuration for Program entity
/// </summary>
public class ProgramConfiguration : IEntityTypeConfiguration<Program> {
  public void Configure(EntityTypeBuilder<Program> builder) {
    // Additional configuration can be added here if needed
  }
}
