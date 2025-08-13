using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using HotChocolate; // GraphQL attributes
using HotChocolate.Types;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Represents a project (game, tool, art, etc.)
/// Enhanced with improved validation, indexing, and relationships
/// </summary>
[Table("Projects")]
[Index(nameof(Title))]
[Index(nameof(Status))]
[Index(nameof(Visibility))]
[Index(nameof(CreatedById))]
[Index(nameof(CategoryId))]
[Index(nameof(CreatedAt))]
[Index(nameof(UpdatedAt))]
public sealed class Project : Content {
  /// <summary>
  /// Short description (max 500 chars)
  /// </summary>
  [GraphQLDescription("Short description (max 500 chars).")]
  [MaxLength(500)]
  public string? ShortDescription { get; set; }

  /// <summary>
  /// Project image/logo URL
  /// </summary>
  [MaxLength(500)]
  public string? ImageUrl { get; set; }

  /// <summary>
  /// Project type (Game, Tool, Art, etc.)
  /// </summary>
  public Common.ProjectType Type { get; set; } = Common.ProjectType.Game;

  /// <summary>
  /// Development status
  /// </summary>
  public DevelopmentStatus DevelopmentStatus { get; set; } = DevelopmentStatus.Planning;

  /// <summary>
  /// Project category (entity)
  /// </summary>
  public ProjectCategory? Category { get; set; }

  public Guid? CategoryId { get; set; }

  /// <summary>
  /// Website URL
  /// </summary>
  [MaxLength(500)]
  public string? WebsiteUrl { get; set; }

  /// <summary>
  /// Repository URL
  /// </summary>
  [MaxLength(500)]
  public string? RepositoryUrl { get; set; }

  /// <summary>
  /// Social links (JSON string)
  /// </summary>
  public string? SocialLinks { get; set; }

  /// <summary>
  /// Download URL or platform links
  /// </summary>
  [MaxLength(500)]
  public string? DownloadUrl { get; set; }

  /// <summary>
  /// Project tags (JSON array)
  /// </summary>
  public string? Tags { get; set; }

  /// <summary>
  /// Project metadata and statistics
  /// </summary>
  public ProjectMetadata? ProjectMetadata { get; set; }

  /// <summary>
  /// Navigation property to project versions
  /// </summary>
  public ICollection<ProjectVersion> Versions { get; set; } = new List<ProjectVersion>();

  /// <summary>
  /// Navigation property to project collaborators
  /// </summary>
  public ICollection<ProjectCollaborator> Collaborators { get; set; } = new List<ProjectCollaborator>();

  /// <summary>
  /// Navigation property to project releases
  /// </summary>
  public ICollection<ProjectRelease> Releases { get; set; } = new List<ProjectRelease>();

  /// <summary>
  /// Navigation property to project teams
  /// </summary>
  public ICollection<ProjectTeam> Teams { get; set; } = new List<ProjectTeam>();

  /// <summary>
  /// Navigation property to project followers
  /// </summary>
  public ICollection<ProjectFollower> Followers { get; set; } = new List<ProjectFollower>();

  /// <summary>
  /// Navigation property to project feedback/reviews
  /// </summary>
  public ICollection<ProjectFeedback> Feedbacks { get; set; } = new List<ProjectFeedback>();

  /// <summary>
  /// Navigation property to jam submissions
  /// </summary>
  public ICollection<ProjectJamSubmission> JamSubmissions { get; set; } = new List<ProjectJamSubmission>();

  /// <summary>
  /// User who created the project
  /// </summary>
  public User? CreatedBy { get; set; }

  public Guid? CreatedById { get; set; }

  /// <summary>
  /// Tenant this project belongs to (for multi-tenancy)
  /// </summary>
  public override Tenant? Tenant { get; set; }

  public Guid? TenantId { get; set; }

  /// <summary>
  /// Computed property: Is the project active
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public bool IsActive {
    get => Status == ContentStatus.Published && DeletedAt == null;
  }

  /// <summary>
  /// Computed property: Latest version
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public ProjectVersion? LatestVersion {
    get => Versions.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
  }

  /// <summary>
  /// Computed property: Number of followers
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public int FollowerCount {
    get => Followers.Count;
  }

  /// <summary>
  /// Computed property: Average rating from feedback
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public decimal? AverageRating {
    get =>
      Feedbacks.Count != 0
        ? (decimal?)Feedbacks.Where(f => f.Status == ContentStatus.Published).Average(f => f.Rating)
        : null;
  }

  /// <summary>
  /// Computed property: Total feedback count
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public int FeedbackCount {
    get => Feedbacks.Count(f => f.Status == ContentStatus.Published);
  }

  /// <summary>
  /// Computed property: Whether the project is part of active jams
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public bool IsInJam {
    get => JamSubmissions.Count != 0;
  }

  /// <summary>
  /// Computed property: Number of teams working on this project
  /// </summary>
  [GraphQLIgnore]
  [NotMapped]
  public int TeamCount {
    get => Teams.Count(team => team.IsActive);
  }

  // Computed property temporarily commented out due to circular reference
  // /// <summary>
  // /// Computed property: Total download count from all releases
  // /// </summary>
  // [NotMapped]
  // public int TotalDownloads
  // {
  //     get => Releases?.Sum(r => r.DownloadCount) ?? 0;
  // }
  /// <summary>
  /// Generate URL-friendly slug from title
  /// </summary>
  public static string GenerateSlug(string title) {
    if (string.IsNullOrWhiteSpace(title)) return string.Empty;

    return title.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Replace(".", "-")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("{", "")
                .Replace("}", "")
                .Replace(",", "")
                .Replace(";", "")
                .Replace(":", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace("@", "")
                .Replace("#", "")
                .Replace("$", "")
                .Replace("%", "")
                .Replace("^", "")
                .Replace("&", "")
                .Replace("*", "")
                .Replace("+", "")
                .Replace("=", "")
                .Replace("|", "")
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("<", "")
                .Replace(">", "");
  }
}
