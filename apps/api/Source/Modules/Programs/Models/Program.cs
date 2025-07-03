using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Certificate.Models;
using GameGuild.Modules.Feedback.Models;
using GameGuild.Modules.Product.Models;
using GameGuild.Modules.Tag.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


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

  private ICollection<ProgramWishlist> _programWishlists = new List<ProgramWishlist>();

  [MaxLength(500)]
  public string? Thumbnail {
    get => _thumbnail;
    set => _thumbnail = value;
  }

  /// <summary>
  /// Video showcase URL for program preview
  /// </summary>
  [MaxLength(500)]
  public string? VideoShowcaseUrl { get; set; }

  /// <summary>
  /// Estimated time load in hours to complete the program
  /// </summary>
  public float? EstimatedHours { get; set; }

  // TODO: Add verification system later
  // - VerificationStatus (enum: NotVerified, GameGuildVerified, CommunityVerified, FullyVerified)
  // - VerifiedAt (DateTime?)
  // - VerifiedBy (string?)
  // - VerificationNote (string?)

  /// <summary>
  /// Enrollment status for the program
  /// </summary>
  public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Open;

  /// <summary>
  /// Maximum number of enrollments allowed (null = unlimited)
  /// </summary>
  public int? MaxEnrollments { get; set; }

  /// <summary>
  /// Enrollment deadline (null = no deadline)
  /// </summary>
  public DateTime? EnrollmentDeadline { get; set; }

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

  public virtual ICollection<ProgramWishlist> ProgramWishlists {
    get => _programWishlists;
    set => _programWishlists = value;
  }

  // Computed properties for skills via Certificates
  /// <summary>
  /// Get all skills required by certificates in this program where RelationshipType is Required
  /// </summary>
  public IEnumerable<CertificateTag> SkillsRequired =>

    Certificates.SelectMany(c => c.CertificateTags.Where(ct => ct.RelationshipType == CertificateTagRelationshipType.Required));


  /// <summary>
  /// Get all skills provided by certificates in this program where RelationshipType is Demonstrates
  /// </summary>
  public IEnumerable<CertificateTag> SkillsProvided =>

    Certificates.SelectMany(c => c.CertificateTags.Where(ct => ct.RelationshipType == CertificateTagRelationshipType.Demonstrates));

  // Computed properties for metrics
  public int CurrentEnrollments => ProgramUsers.Count(pu => pu.IsActive);


  public decimal AverageRating => ProgramRatings.Any() ? ProgramRatings.Average(pr => pr.Rating) : 0;


  public int TotalRatings => ProgramRatings.Count;

  public bool IsEnrollmentOpen => EnrollmentStatus == EnrollmentStatus.Open &&

    (MaxEnrollments == null || CurrentEnrollments < MaxEnrollments) &&
    (EnrollmentDeadline == null || EnrollmentDeadline > DateTime.UtcNow);

  /// <summary>
  /// Calculate estimated weeks to complete based on hours per week load
  /// </summary>
  /// <param name="hoursPerWeek">Number of hours user can dedicate per week</param>
  /// <returns>Estimated weeks to completion, or null if EstimatedHours is not set</returns>
  public float? GetEstimatedWeeks(int hoursPerWeek) {
    if (EstimatedHours == null || EstimatedHours <= 0 || hoursPerWeek <= 0)
      return null;

    return (float)Math.Ceiling((double)EstimatedHours.Value / hoursPerWeek);
  }

  /// <summary>
  /// Get skills required as TagProficiencies with their proficiency levels
  /// </summary>
  public IEnumerable<TagProficiency> GetRequiredSkills() {
    return SkillsRequired.Select(ct => ct.Tag);
  }

  /// <summary>
  /// Get skills provided as TagProficiencies with their proficiency levels
  /// </summary>
  public IEnumerable<TagProficiency> GetProvidedSkills() {
    return SkillsProvided.Select(ct => ct.Tag);
  }

  // Helper methods for JSON metadata
  public T? GetMetadata<T>(string key) where T : class {
    if (Metadata?.AdditionalData == null) return null;

    try {
      var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData);

      if (metadataDict != null && metadataDict.TryGetValue(key, out var value)) return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  public void SetMetadata<T>(string key, T value) {
    if (Metadata == null) Metadata = new ResourceMetadata { ResourceType = nameof(Program), AdditionalData = "{}" };

    var metadataDict = string.IsNullOrEmpty(Metadata.AdditionalData)
                         ? new Dictionary<string, object>()
                         : JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData) ??
                           new Dictionary<string, object>();

    metadataDict[key] = value!;
    Metadata.AdditionalData = JsonSerializer.Serialize(metadataDict);
  }

  // TODO: Add verification helper methods later
  // - MarkAsVerified(VerificationStatus status, string verifiedBy, string? note = null)
  // - RemoveVerification()

  // Calculate estimated weeks based on hours per week
  public float? CalculateEstimatedWeeks(int hoursPerWeek) {
    if (EstimatedHours == null || EstimatedHours <= 0 || hoursPerWeek <= 0) return null;
    return (float)Math.Ceiling((double)EstimatedHours.Value / hoursPerWeek);
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
