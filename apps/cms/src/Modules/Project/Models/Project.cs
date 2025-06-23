using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Project.Models;

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
public class Project : Content
{
    private string? _shortDescription;

    private string? _imageUrl;

    private ProjectType _type = ProjectType.Game;

    private DevelopmentStatus _developmentStatus = DevelopmentStatus.Planning;

    private ProjectCategory? _category;

    private Guid? _categoryId;

    private string? _websiteUrl;

    private string? _repositoryUrl;

    private string? _socialLinks;

    private string? _downloadUrl;

    private string? _tags;

    private ProjectMetadata? _projectMetadata;

    private ICollection<ProjectVersion> _versions = new List<ProjectVersion>();

    private ICollection<ProjectCollaborator> _collaborators = new List<ProjectCollaborator>();

    private ICollection<ProjectRelease> _releases = new List<ProjectRelease>();

    private ICollection<ProjectTeam> _teams = new List<ProjectTeam>();

    private ICollection<ProjectFollower> _followers = new List<ProjectFollower>();

    private ICollection<ProjectFeedback> _feedbacks = new List<ProjectFeedback>();

    private ICollection<ProjectJamSubmission> _jamSubmissions = new List<ProjectJamSubmission>();

    private User.Models.User? _createdBy;

    private Guid? _createdById;

    private Tenant.Models.Tenant? _tenant1;

    private Guid? _tenantId;

    /// <summary>
    /// Short description (max 500 chars)
    /// </summary>
    [MaxLength(500)]
    public string? ShortDescription
    {
        get => _shortDescription;
        set => _shortDescription = value;
    }

    /// <summary>
    /// Project image/logo URL
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl
    {
        get => _imageUrl;
        set => _imageUrl = value;
    }

    /// <summary>
    /// Project type (Game, Tool, Art, etc.)
    /// </summary>
    public ProjectType Type
    {
        get => _type;
        set => _type = value;
    }

    /// <summary>
    /// Development status
    /// </summary>
    public DevelopmentStatus DevelopmentStatus
    {
        get => _developmentStatus;
        set => _developmentStatus = value;
    }

    /// <summary>
    /// Project category (entity)
    /// </summary>
    public virtual ProjectCategory? Category
    {
        get => _category;
        set => _category = value;
    }

    public Guid? CategoryId
    {
        get => _categoryId;
        set => _categoryId = value;
    }

    /// <summary>
    /// Website URL
    /// </summary>
    [MaxLength(500)]
    public string? WebsiteUrl
    {
        get => _websiteUrl;
        set => _websiteUrl = value;
    }

    /// <summary>
    /// Repository URL
    /// </summary>
    [MaxLength(500)]
    public string? RepositoryUrl
    {
        get => _repositoryUrl;
        set => _repositoryUrl = value;
    }

    /// <summary>
    /// Social links (JSON string)
    /// </summary>
    public string? SocialLinks
    {
        get => _socialLinks;
        set => _socialLinks = value;
    }

    /// <summary>
    /// Download URL or platform links
    /// </summary>
    [MaxLength(500)]
    public string? DownloadUrl
    {
        get => _downloadUrl;
        set => _downloadUrl = value;
    }

    /// <summary>
    /// Project tags (JSON array)
    /// </summary>
    public string? Tags
    {
        get => _tags;
        set => _tags = value;
    }

    /// <summary>
    /// Project metadata and statistics
    /// </summary>
    public virtual ProjectMetadata? ProjectMetadata
    {
        get => _projectMetadata;
        set => _projectMetadata = value;
    }

    /// <summary>
    /// Navigation property to project versions
    /// </summary>
    public virtual ICollection<ProjectVersion> Versions
    {
        get => _versions;
        set => _versions = value;
    }

    /// <summary>
    /// Navigation property to project collaborators
    /// </summary>
    public virtual ICollection<ProjectCollaborator> Collaborators
    {
        get => _collaborators;
        set => _collaborators = value;
    }

    /// <summary>
    /// Navigation property to project releases
    /// </summary>
    public virtual ICollection<ProjectRelease> Releases
    {
        get => _releases;
        set => _releases = value;
    }

    /// <summary>
    /// Navigation property to project teams
    /// </summary>
    public virtual ICollection<ProjectTeam> Teams
    {
        get => _teams;
        set => _teams = value;
    }

    /// <summary>
    /// Navigation property to project followers
    /// </summary>
    public virtual ICollection<ProjectFollower> Followers
    {
        get => _followers;
        set => _followers = value;
    }

    /// <summary>
    /// Navigation property to project feedback/reviews
    /// </summary>
    public virtual ICollection<ProjectFeedback> Feedbacks
    {
        get => _feedbacks;
        set => _feedbacks = value;
    }

    /// <summary>
    /// Navigation property to jam submissions
    /// </summary>
    public virtual ICollection<ProjectJamSubmission> JamSubmissions
    {
        get => _jamSubmissions;
        set => _jamSubmissions = value;
    }

    /// <summary>
    /// User who created the project
    /// </summary>
    public virtual User.Models.User? CreatedBy
    {
        get => _createdBy;
        set => _createdBy = value;
    }

    public Guid? CreatedById
    {
        get => _createdById;
        set => _createdById = value;
    }

    /// <summary>
    /// Tenant this project belongs to (for multi-tenancy)
    /// </summary>
    public override Tenant.Models.Tenant? Tenant
    {
        get => _tenant1;
        set => _tenant1 = value;
    }

    public Guid? TenantId
    {
        get => _tenantId;
        set => _tenantId = value;
    }

    /// <summary>
    /// Computed property: Is project active
    /// </summary>
    [NotMapped]
    public bool IsActive
    {
        get => Status == ContentStatus.Published && !IsDeleted;
    }

    /// <summary>
    /// Computed property: Latest version
    /// </summary>
    [NotMapped]
    public ProjectVersion? LatestVersion
    {
        get => Versions?.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Computed property: Number of followers
    /// </summary>
    [NotMapped]
    public int FollowerCount
    {
        get => Followers?.Count ?? 0;
    }

    /// <summary>
    /// Computed property: Average rating from feedback
    /// </summary>
    [NotMapped]
    public decimal? AverageRating
    {
        get => Feedbacks?.Any() == true
            ? (decimal?)Feedbacks.Where(f => f.Status == ContentStatus.Published).Average(f => f.Rating)
            : null;
    }

    /// <summary>
    /// Computed property: Total feedback count
    /// </summary>
    [NotMapped]
    public int FeedbackCount
    {
        get => Feedbacks?.Count(f => f.Status == ContentStatus.Published) ?? 0;
    }

    /// <summary>
    /// Computed property: Whether project is part of active jams
    /// </summary>
    [NotMapped]
    public bool IsInJam
    {
        get => JamSubmissions?.Any() == true;
    }

    /// <summary>
    /// Computed property: Number of teams working on this project
    /// </summary>
    [NotMapped] public int TeamCount
    {
        get => Teams?.Count(t => t.IsActive) ?? 0;
    }

    // Computed property temporarily commented out due to circular reference
    // /// <summary>
    // /// Computed property: Total download count from all releases
    // /// </summary>
    // [NotMapped]
    // public int TotalDownloads
    // {
    //     get => Releases?.Sum(r => r.DownloadCount) ?? 0;
    // }    /// <summary>
    /// Generate URL-friendly slug from title
    /// </summary>
    public static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

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

/// <summary>
/// Entity configuration for Project
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // Table configuration
        builder.ToTable("Projects");

        // Properties
        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.ShortDescription)
            .HasMaxLength(500);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(p => p.RepositoryUrl)
            .HasMaxLength(500);

        builder.Property(p => p.DownloadUrl)
            .HasMaxLength(500);

        // JSON properties
        builder.Property(p => p.SocialLinks)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<string>(v, (JsonSerializerOptions?)null)
            );

        builder.Property(p => p.Tags)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<string>(v, (JsonSerializerOptions?)null)
            );

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(p => p.Versions)
            .WithOne(v => v.Project)
            .HasForeignKey(v => v.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Collaborators)
            .WithOne(c => c.Project)
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Teams)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Followers)
            .WithOne(f => f.Project)
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Feedbacks)
            .WithOne(f => f.Project)
            .HasForeignKey(f => f.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.JamSubmissions)
            .WithOne(js => js.Project)
            .HasForeignKey(js => js.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(p => p.Title);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.Visibility);
        builder.HasIndex(p => p.CreatedById);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.UpdatedAt);
        builder.HasIndex(p => new
            {
                p.Status, p.Visibility
            }
        );
        builder.HasIndex(p => new
            {
                p.CategoryId, p.Status
            }
        );
        builder.HasIndex(p => new
            {
                p.TenantId, p.Status
            }
        );
    }
}
