using System.Text.Json;
using GameGuild.Domain.Common;
using GameGuild.Modules.Products.Entities;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Feedbacks.Entities;

[Table("program_feedback_submissions")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(ProductId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(OverallRating))]
[Index(nameof(SubmittedAt))]
[Index(nameof(IsAnonymous))]
[Index(nameof(Category))]
public class ProgramFeedbackSubmission : EntityBase
{
    public Guid UserId { get; set; }

    public Guid ProgramId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid ProgramUserId { get; set; }

    /// <summary>
    /// Feedback responses stored as JSON
    /// Structure: {questionId: response, questionId: response, ...}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string FeedbackData { get; set; } = "{}";

    /// <summary>
    /// Overall satisfaction rating (1-5)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal? OverallRating { get; set; }

    /// <summary>
    /// General comments about the program
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Whether the user would recommend this program
    /// </summary>
    public bool? WouldRecommend { get; set; }

    /// <summary>
    /// Date when feedback was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Whether the feedback is submitted anonymously
    /// </summary>
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// Feedback category for classification
    /// </summary>
    public FeedbackCategory Category { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Program Program { get; set; } = null!;

    public virtual Product? Product { get; set; }

    public virtual ProgramUser ProgramUser { get; set; } = null!;

    // Helper methods for JSON feedback data
    public T? GetFeedbackResponse<T>(string questionId) where T : class
    {
        if (string.IsNullOrEmpty(FeedbackData)) return null;

        try
        {
            var json = JsonDocument.Parse(FeedbackData);

            if (json.RootElement.TryGetProperty(questionId, out var element))
                return JsonSerializer.Deserialize<T>(element.GetRawText());
        }
        catch
        {
            // Handle JSON parsing errors gracefully
        }

        return null;
    }

    public void SetFeedbackResponse<T>(string questionId, T value)
    {
        var data = string.IsNullOrEmpty(FeedbackData)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(FeedbackData) ?? new Dictionary<string, object>();

        data[questionId] = value!;
        FeedbackData = JsonSerializer.Serialize(data);
    }
}
