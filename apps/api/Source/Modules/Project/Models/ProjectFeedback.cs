using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents feedback/review for a project
/// </summary>
[Table("ProjectFeedbacks")]
[Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = "IX_ProjectFeedbacks_Project_User")]
[Index(nameof(ProjectId), nameof(Rating), Name = "IX_ProjectFeedbacks_Project_Rating")]
[Index(nameof(UserId), Name = "IX_ProjectFeedbacks_User")]
[Index(nameof(CreatedAt), Name = "IX_ProjectFeedbacks_Date")]
public class ProjectFeedback : ResourceBase {
  private Guid _projectId;

  private Project _project = null!;

  private Guid _userId;

  private User.Models.User _user = null!;

  private int _rating = 5;

  private string _title = string.Empty;

  private string? _content;

  private string? _categories;

  private bool _isFeatured = false;

  private bool _isVerified = false;

  private ContentStatus _status = ContentStatus.Published;

  private int _helpfulVotes = 0;

  private int _totalVotes = 0;

  private string? _platform;

  private string? _projectVersion;

  /// <summary>
  /// Project being reviewed
  /// </summary>
  public Guid ProjectId {
    get => _projectId;
    set => _projectId = value;
  }

  /// <summary>
  /// Navigation property to project
  /// </summary>
  public virtual Project Project {
    get => _project;
    set => _project = value;
  }

  /// <summary>
  /// User providing feedback
  /// </summary>
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Navigation property to user
  /// </summary>
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Rating (1-5 stars)
  /// </summary>
  [Range(1, 5)]
  public int Rating {
    get => _rating;
    set => _rating = value;
  }

  /// <summary>
  /// Review title
  /// </summary>
  [Required]
  [MaxLength(200)]
  public new string Title {
    get => _title;
    set => _title = value;
  }

  /// <summary>
  /// Review content
  /// </summary>
  [MaxLength(2000)]
  public string? Content {
    get => _content;
    set => _content = value;
  }

  /// <summary>
  /// Feedback categories (JSON array)
  /// </summary>
  [MaxLength(500)]
  public string? Categories {
    get => _categories;
    set => _categories = value;
  }

  /// <summary>
  /// Whether this feedback is featured
  /// </summary>
  public bool IsFeatured {
    get => _isFeatured;
    set => _isFeatured = value;
  }

  /// <summary>
  /// Whether this feedback has been verified (e.g., from actual user)
  /// </summary>
  public bool IsVerified {
    get => _isVerified;
    set => _isVerified = value;
  }

  /// <summary>
  /// Feedback status
  /// </summary>
  public ContentStatus Status {
    get => _status;
    set => _status = value;
  }

  /// <summary>
  /// Number of helpful votes
  /// </summary>
  public int HelpfulVotes {
    get => _helpfulVotes;
    set => _helpfulVotes = value;
  }

  /// <summary>
  /// Number of total votes
  /// </summary>
  public int TotalVotes {
    get => _totalVotes;
    set => _totalVotes = value;
  }

  /// <summary>
  /// Platform where the project was experienced (if applicable)
  /// </summary>
  [MaxLength(100)]
  public string? Platform {
    get => _platform;
    set => _platform = value;
  }

  /// <summary>
  /// Version of the project this feedback is for
  /// </summary>
  [MaxLength(50)]
  public string? ProjectVersion {
    get => _projectVersion;
    set => _projectVersion = value;
  }
}
