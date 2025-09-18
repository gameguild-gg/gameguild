using System.Text.Json;
using GameGuild.Modules.Certificates;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Feedbacks;
using GameGuild.Modules.Products;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tags.Models;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Represents a learning program that contains structured educational content and enrollment management
/// </summary>
/// <remarks>
/// A Program serves as a container for educational content including lessons, assignments, quizzes,
/// and activities. It manages user enrollments, progress tracking, certification pathways,
/// and provides analytics for learner performance and engagement.
/// 
/// Programs can be categorized by domain (Programming, DataScience, etc.) and difficulty level,
/// with support for hierarchical content organization and flexible grading methods.
/// </remarks>
[Table("programs")]
[Index(nameof(Visibility))]
[Index(nameof(Status))]
[Index(nameof(Slug))]
[Index(nameof(Category))]
[Index(nameof(Difficulty))]
public class Program : Content {
  /// <summary>
  /// Thumbnail image URL for program display in listings and cards
  /// </summary>
  /// <remarks>
  /// Should be optimized for display at various sizes. Recommended dimensions: 16:9 aspect ratio.
  /// </remarks>
  [MaxLength(500)]
  public string? Thumbnail { get; set; }

  /// <summary>
  /// Video showcase URL for program preview and marketing
  /// </summary>
  /// <remarks>
  /// Provides prospective learners with a preview of program content and structure.
  /// Should be a publicly accessible video URL (YouTube, Vimeo, etc.)
  /// </remarks>
  [MaxLength(500)]
  public string? VideoShowcaseUrl { get; set; }

  /// <summary>
  /// Estimated time commitment in hours required to complete the entire program
  /// </summary>
  /// <remarks>
  /// Used for learner planning and expectation setting. Calculated based on
  /// individual content piece estimates and complexity factors.
  /// </remarks>
  public float? EstimatedHours { get; set; }

  // TODO: Add verification system later
  // - VerificationStatus (enum: NotVerified, GameGuildVerified, CommunityVerified, FullyVerified)
  // - VerifiedAt (DateTime?)
  // - VerifiedBy (string?)
  // - VerificationNote (string?)

  /// <summary>
  /// Current enrollment status controlling whether new learners can join the program
  /// </summary>
  /// <remarks>
  /// Controls program accessibility: Open (accepting enrollments), Closed (no new enrollments),
  /// or WaitingList (queue-based enrollment when capacity is reached).
  /// </remarks>
  public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Open;

  /// <summary>
  /// Maximum number of concurrent enrollments allowed (null indicates unlimited capacity)
  /// </summary>
  /// <remarks>
  /// Used to manage program capacity and maintain quality of instruction.
  /// When reached, enrollment status may automatically switch to WaitingList.
  /// </remarks>
  public int? MaxEnrollments { get; set; }

  /// <summary>
  /// Deadline for new enrollments (null indicates no enrollment deadline)
  /// </summary>
  /// <remarks>
  /// After this date, new enrollments are automatically blocked regardless of
  /// enrollment status. Existing enrolled learners are not affected.
  /// </remarks>
  public DateTime? EnrollmentDeadline { get; set; }

  /// <summary>
  /// Educational domain category for program classification and discovery
  /// </summary>
  /// <remarks>
  /// Enables filtering and categorization in program listings.
  /// Examples: Programming, DataScience, GameDevelopment, etc.
  /// </remarks>
  public ProgramCategory Category { get; set; } = ProgramCategory.Other;

  /// <summary>
  /// Difficulty level indicating prerequisite knowledge and complexity
  /// </summary>
  /// <remarks>
  /// Helps learners select appropriate programs based on their experience level.
  /// Ranges from Beginner (no prerequisites) to Expert (advanced domain knowledge).
  /// </remarks>
  public ProgramDifficulty Difficulty { get; set; } = ProgramDifficulty.Beginner;

  // Navigation properties

  /// <summary>
  /// Collection of content items (lessons, assignments, quizzes) within this program
  /// </summary>
  /// <remarks>
  /// Represents the structured learning content organized hierarchically.
  /// Content can be nested (modules containing lessons) and ordered by SortOrder.
  /// </remarks>
  public virtual ICollection<ProgramContent> ProgramContents { get; set; } = [];

  /// <summary>
  /// Collection of user enrollments and their participation in this program
  /// </summary>
  /// <remarks>
  /// Tracks learner enrollment status, progress, completion, and role assignments.
  /// Used for access control and progress analytics.
  /// </remarks>
  public virtual ICollection<ProgramUser> ProgramUsers { get; set; } = [];

  /// <summary>
  /// Collection of product associations for monetization and access control
  /// </summary>
  /// <remarks>
  /// Links programs to purchasable products for premium content access.
  /// Enables subscription-based or one-time purchase program access models.
  /// </remarks>
  public virtual ICollection<ProductProgram> ProductPrograms { get; set; } = [];

  /// <summary>
  /// Collection of certificates that can be earned through this program
  /// </summary>
  /// <remarks>
  /// Represents credentials and skills verification available upon program completion.
  /// Certificates define required competencies and learning outcomes.
  /// </remarks>
  public virtual ICollection<Certificate> Certificates { get; set; } = [];

  /// <summary>
  /// Collection of feedback submissions for program evaluation and improvement
  /// </summary>
  /// <remarks>
  /// Enables continuous program improvement through learner feedback collection.
  /// Used for quality assurance and instructional design enhancement.
  /// </remarks>
  public virtual ICollection<ProgramFeedbackSubmission> FeedbackSubmissions { get; set; } = [];

  /// <summary>
  /// Collection of user ratings for program quality and satisfaction metrics
  /// </summary>
  /// <remarks>
  /// Provides social proof and quality indicators for program discovery.
  /// Used for calculating average ratings and recommendation algorithms.
  /// </remarks>
  public virtual ICollection<ProgramRating> ProgramRatings { get; set; } = [];

  /// <summary>
  /// Collection of user wishlist entries indicating interest in this program
  /// </summary>
  /// <remarks>
  /// Tracks user interest for marketing and demand analysis.
  /// Used for notifications when programs become available or go on sale.
  /// </remarks>
  public virtual ICollection<ProgramWishlist> ProgramWishlists { get; set; } = [];

  // Computed properties for skills via Certificates

  /// <summary>
  /// Gets all prerequisite skills required for successful program completion
  /// </summary>
  /// <remarks>
  /// Derived from certificates where RelationshipType is Required.
  /// Used for program prerequisites and learner readiness assessment.
  /// </remarks>
  public IEnumerable<CertificateTag> SkillsRequired {
    get => Certificates.SelectMany(c => c.CertificateTags
        .Where(ct => ct.RelationshipType == CertificateTagRelationshipType.Required));
  }

  /// <summary>
  /// Gets all skills and competencies that learners will demonstrate upon program completion
  /// </summary>
  /// <remarks>
  /// Derived from certificates where RelationshipType is Demonstrates.
  /// Used for learning outcomes and career pathway mapping.
  /// </remarks>
  public IEnumerable<CertificateTag> SkillsProvided {
    get => Certificates.SelectMany(c => c.CertificateTags
        .Where(ct => ct.RelationshipType == CertificateTagRelationshipType.Demonstrates));
  }

  // Computed properties for metrics and enrollment status

  /// <summary>
  /// Gets the current count of active enrollments in this program
  /// </summary>
  /// <remarks>
  /// Excludes inactive, withdrawn, or completed enrollments.
  /// Used for capacity management and enrollment availability checks.
  /// </remarks>
  public int CurrentEnrollments {
    get => ProgramUsers.Count(pu => pu.IsActive);
  }

  /// <summary>
  /// Calculates the average rating from all user ratings (0 if no ratings exist)
  /// </summary>
  /// <remarks>
  /// Provides quality indicator for program discovery and selection.
  /// Returns 0 for programs with no ratings to avoid null reference issues.
  /// </remarks>
  public decimal AverageRating {
    get => ProgramRatings.Count != 0 ? ProgramRatings.Average(pr => pr.Rating) : 0;
  }

  /// <summary>
  /// Gets the total number of ratings submitted for this program
  /// </summary>
  /// <remarks>
  /// Used for social proof and rating reliability indicators.
  /// Higher counts generally indicate more reliable average ratings.
  /// </remarks>
  public int TotalRatings {
    get => ProgramRatings.Count;
  }

  /// <summary>
  /// Determines if the program is currently accepting new enrollments
  /// </summary>
  /// <remarks>
  /// Considers enrollment status, capacity limits, and deadline constraints.
  /// Returns false if any blocking condition exists (closed, full, expired).
  /// </remarks>
  public bool IsEnrollmentOpen {
    get => EnrollmentStatus == EnrollmentStatus.Open &&
           (MaxEnrollments == null || CurrentEnrollments < MaxEnrollments) &&
           (EnrollmentDeadline == null || EnrollmentDeadline > DateTime.UtcNow);
  }

  /// <summary>
  /// Calculates estimated weeks to complete the program based on learner's time commitment
  /// </summary>
  /// <param name="hoursPerWeek">Number of hours user can dedicate per week</param>
  /// <returns>Estimated weeks to completion, or null if EstimatedHours is not set or invalid parameters</returns>
  /// <remarks>
  /// Uses ceiling function to provide conservative estimates.
  /// Returns null for invalid inputs to handle edge cases gracefully.
  /// </remarks>
  public float? GetEstimatedWeeks(int hoursPerWeek) {
    // Validate inputs to prevent division by zero and ensure meaningful calculations
    if (EstimatedHours is null or <= 0 || hoursPerWeek <= 0) {
      return null;
    }

    // Use ceiling to provide conservative time estimates
    return (float)Math.Ceiling((double)EstimatedHours.Value / hoursPerWeek);
  }

  /// <summary>
  /// Gets prerequisite skills as TagProficiency entities for detailed competency mapping
  /// </summary>
  /// <returns>Collection of required skills with proficiency levels</returns>
  /// <remarks>
  /// Provides detailed skill requirements for learner readiness assessment
  /// and prerequisite course recommendations.
  /// </remarks>
  public IEnumerable<TagProficiency> GetRequiredSkills() {
    return SkillsRequired.Select(ct => ct.Tag);
  }

  /// <summary>
  /// Gets learning outcome skills as TagProficiency entities for career pathway planning
  /// </summary>
  /// <returns>Collection of skills learners will demonstrate upon completion</returns>
  /// <remarks>
  /// Used for career pathway mapping, credential verification,
  /// and next-step course recommendations.
  /// </remarks>
  public IEnumerable<TagProficiency> GetProvidedSkills() {
    return SkillsProvided.Select(ct => ct.Tag);
  }

  // Helper methods for JSON metadata management

  /// <summary>
  /// Retrieves strongly-typed metadata value by key from the program's metadata store
  /// </summary>
  /// <typeparam name="T">Type to deserialize the metadata value to</typeparam>
  /// <param name="key">Metadata key identifier</param>
  /// <returns>Deserialized metadata value or null if key not found or deserialization fails</returns>
  /// <remarks>
  /// Provides type-safe access to program-specific configuration and extended properties.
  /// Gracefully handles JSON parsing errors and missing keys.
  /// </remarks>
  public T? GetMetadata<T>(string key) where T : class {
    // Return null if no metadata exists
    if (Metadata?.AdditionalData is null) {
      return null;
    }

    try {
      // Parse metadata JSON into dictionary for key-based access
      var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData);

      // Attempt to retrieve and deserialize the requested key
      if (metadataDict != null && metadataDict.TryGetValue(key, out var value)) {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
      }
    }
    catch {
      // Handle JSON parsing errors gracefully by returning null
    }

    return null;
  }

  /// <summary>
  /// Sets strongly-typed metadata value by key in the program's metadata store
  /// </summary>
  /// <typeparam name="T">Type of the metadata value to store</typeparam>
  /// <param name="key">Metadata key identifier</param>
  /// <param name="value">Value to store in metadata</param>
  /// <remarks>
  /// Initializes metadata if it doesn't exist and safely merges new values
  /// with existing metadata dictionary.
  /// </remarks>
  public void SetMetadata<T>(string key, T value) {
    // Initialize metadata if it doesn't exist
    if (Metadata is null) {
      Metadata = new ResourceMetadata {
        ResourceType = nameof(Program),
        AdditionalData = "{}"
      };
    }

    // Parse existing metadata or create empty dictionary
    var metadataDict = string.IsNullOrEmpty(Metadata.AdditionalData)
      ? []
      : JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata.AdditionalData) ?? [];

    // Set the new value and serialize back to JSON
    metadataDict[key] = value!;
    Metadata.AdditionalData = JsonSerializer.Serialize(metadataDict);
  }

  // TODO: Add verification helper methods later
  // - MarkAsVerified(VerificationStatus status, string verifiedBy, string? note = null)
  // - RemoveVerification()

  /// <summary>
  /// Calculates estimated weeks to complete the program based on learner's weekly time commitment
  /// </summary>
  /// <param name="hoursPerWeek">Number of hours learner can dedicate per week</param>
  /// <returns>Estimated weeks to completion, or null if EstimatedHours is not set or invalid parameters</returns>
  /// <remarks>
  /// Alternative method name for GetEstimatedWeeks with identical functionality.
  /// Uses conservative estimates with ceiling function for planning purposes.
  /// </remarks>
  public float? CalculateEstimatedWeeks(int hoursPerWeek) {
    // Validate inputs to ensure meaningful time calculations
    if (EstimatedHours is null or <= 0 || hoursPerWeek <= 0) {
      return null;
    }

    // Provide conservative time estimates using ceiling function
    return (float)Math.Ceiling((double)EstimatedHours.Value / hoursPerWeek);
  }
}
