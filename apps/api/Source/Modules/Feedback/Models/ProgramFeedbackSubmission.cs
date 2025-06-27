using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Modules.Program.Models;

namespace GameGuild.Modules.Feedback.Models;

[Table("program_feedback_submissions")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(ProductId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(OverallRating))]
[Index(nameof(SubmittedAt))]
public class ProgramFeedbackSubmission : BaseEntity {
  private Guid _userId;

  private Guid _programId;

  private Guid? _productId;

  private Guid _programUserId;

  private string _feedbackData = "{}";

  private decimal? _overallRating;

  private string? _comments;

  private bool? _wouldRecommend;

  private DateTime _submittedAt;

  private User.Models.User _user = null!;

  private Program.Models.Program _program = null!;

  private Product.Models.Product? _product;

  private ProgramUser _programUser = null!;

  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  public Guid ProgramId {
    get => _programId;
    set => _programId = value;
  }

  public Guid? ProductId {
    get => _productId;
    set => _productId = value;
  }

  public Guid ProgramUserId {
    get => _programUserId;
    set => _programUserId = value;
  }

  /// <summary>
  /// Feedback responses stored as JSON
  /// Structure: {questionId: response, questionId: response, ...}
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string FeedbackData {
    get => _feedbackData;
    set => _feedbackData = value;
  }

  /// <summary>
  /// Overall satisfaction rating (1-5)
  /// </summary>
  [Column(TypeName = "decimal(2,1)")]
  public decimal? OverallRating {
    get => _overallRating;
    set => _overallRating = value;
  }

  /// <summary>
  /// General comments about the program
  /// </summary>
  public string? Comments {
    get => _comments;
    set => _comments = value;
  }

  /// <summary>
  /// Whether the user would recommend this program
  /// </summary>
  public bool? WouldRecommend {
    get => _wouldRecommend;
    set => _wouldRecommend = value;
  }

  /// <summary>
  /// Date when feedback was submitted
  /// </summary>
  public DateTime SubmittedAt {
    get => _submittedAt;
    set => _submittedAt = value;
  }

  // Navigation properties
  [ForeignKey(nameof(UserId))]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  [ForeignKey(nameof(ProgramId))]
  public virtual Program.Models.Program Program {
    get => _program;
    set => _program = value;
  }

  [ForeignKey(nameof(ProductId))]
  public virtual Product.Models.Product? Product {
    get => _product;
    set => _product = value;
  }

  [ForeignKey(nameof(ProgramUserId))]
  public virtual Program.Models.ProgramUser ProgramUser {
    get => _programUser;
    set => _programUser = value;
  }

  // Helper methods for JSON feedback data
  public T? GetFeedbackResponse<T>(string questionId) where T : class {
    if (string.IsNullOrEmpty(FeedbackData)) return null;

    try {
      JsonDocument json = JsonDocument.Parse(FeedbackData);

      if (json.RootElement.TryGetProperty(questionId, out JsonElement element)) { return JsonSerializer.Deserialize<T>(element.GetRawText()); }
    }
    catch {
      // Handle JSON parsing errors gracefully
    }

    return null;
  }

  public void SetFeedbackResponse<T>(string questionId, T value) {
    var data = string.IsNullOrEmpty(FeedbackData) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(FeedbackData) ?? new Dictionary<string, object>();

    data[questionId] = value!;
    FeedbackData = JsonSerializer.Serialize(data);
  }
}

public class ProgramFeedbackSubmissionConfiguration : IEntityTypeConfiguration<ProgramFeedbackSubmission> {
  public void Configure(EntityTypeBuilder<ProgramFeedbackSubmission> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pfs => pfs.Program).WithMany().HasForeignKey(pfs => pfs.ProgramId).OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(pfs => pfs.User).WithMany().HasForeignKey(pfs => pfs.UserId).OnDelete(DeleteBehavior.Cascade);

    // Configure optional relationship with Product (can't be done with annotations)
    builder.HasOne(pfs => pfs.Product).WithMany().HasForeignKey(pfs => pfs.ProductId).OnDelete(DeleteBehavior.SetNull);

    // Configure relationship with ProgramUser (can't be done with annotations)
    builder.HasOne(pfs => pfs.ProgramUser).WithMany().HasForeignKey(pfs => pfs.ProgramUserId).OnDelete(DeleteBehavior.Cascade);
  }
}
