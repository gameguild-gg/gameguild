using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// using GameGuild.Modules.Contents.Models;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Represents structured content within a learning program with hierarchical organization and progress tracking
/// </summary>
[Table("program_contents")]
[Index(nameof(ProgramId))]
[Index(nameof(ParentId))]
[Index(nameof(SortOrder))]
[Index(nameof(Type))]
[Index(nameof(IsRequired))]
[Index(nameof(TenantId))]
public class ProgramContent : EntityBase
{
    /// <summary>
    /// Foreign key to the parent program
    /// </summary>
    [Required]
    public Guid ProgramId { get; set; }

    /// <summary>
    /// Parent content item (for hierarchical structure)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Content title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Content description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Content type (lesson, quiz, assignment, etc.)
    /// </summary>
    public ProgramContentType Type { get; set; } = ProgramContentType.Lesson;

    /// <summary>
    /// Content body/text. Holds text-shaped body for lessons whose <see cref="LessonFormat"/> is not
    /// <see cref="LessonContentFormat.Lexical"/>, and for non-structured non-lesson content.
    /// Mutually exclusive with <see cref="JsonBody"/>; enforced by <see cref="NormalizeLearningContract"/>.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Structured content body (jsonb): Lexical lesson state, Questionnaire/Code/Project payloads.
    /// Mutually exclusive with <see cref="Body"/>; enforced by <see cref="NormalizeLearningContract"/>.
    /// </summary>
    public string? JsonBody { get; set; }

    /// <summary>
    /// Authoring and rendering format for lesson content. Non-lesson content does not use this value.
    /// </summary>
    public LessonContentFormat? LessonFormat { get; set; } = LessonContentFormat.Markdown;

    /// <summary>
    /// Serialized typed settings for discussion, reflection, and survey activities.
    /// Legacy rows remain readable when this value is null.
    /// </summary>
    public string? ActivitySettingsData { get; private set; }

    /// <summary>
    /// Sort order within parent or program
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Whether this content is required for program completion
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Grading method for this content
    /// </summary>
    public GradingMethod GradingMethod { get; set; } = GradingMethod.None;

    /// <summary>
    /// Maximum points available for this content
    /// </summary>
    public int? MaxPoints { get; set; }

    /// <summary>
    /// Estimated time to complete in minutes
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// Content visibility level
    /// </summary>
    public Visibility Visibility { get; set; } = Visibility.Public;

    // Navigation Properties
    /// <summary>
    /// Parent program
    /// </summary>
    public virtual Program Program { get; set; } = null!;

    /// <summary>
    /// Parent content item
    /// </summary>
    public virtual ProgramContent? Parent { get; set; }

    /// <summary>
    /// Child content items
    /// </summary>
    public virtual ICollection<ProgramContent> Children { get; set; } = new List<ProgramContent>();

    /// <summary>
    /// Content interactions (progress tracking)
    /// </summary>
    public virtual ICollection<ContentInteraction> ContentInteractions { get; set; } = new List<ContentInteraction>();

    // Computed Properties
    /// <summary>
    /// Whether this content is global (tenant-independent)
    /// </summary>
    public override bool IsGlobal => TenantId == null;

    /// <summary>
    /// Total number of child content items
    /// </summary>
    public int ChildCount => Children?.Count ?? 0;

    /// <summary>
    /// Whether this content has children
    /// </summary>
    public bool HasChildren => ChildCount > 0;

    /// <summary>
    /// Full hierarchical path for display
    /// </summary>
    public string FullPath
    {
        get
        {
            var path = new List<string>();
            var current = this;
            while (current != null)
            {
                path.Insert(0, current.Title);
                current = current.Parent;
            }
            return string.Join(" > ", path);
        }
    }

    // Domain Methods
    /// <summary>
    /// Moves this content to a new parent
    /// </summary>
    public void MoveTo(Guid? newParentId, int newSortOrder)
    {
        ParentId = newParentId;
        SortOrder = newSortOrder;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Reorders this content
    /// </summary>
    public void Reorder(int newSortOrder)
    {
        SortOrder = newSortOrder;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Makes this content required
    /// </summary>
    public void MakeRequired()
    {
        IsRequired = true;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Makes this content optional
    /// </summary>
    public void MakeOptional()
    {
        IsRequired = false;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Sets grading configuration
    /// </summary>
    public void SetGrading(GradingMethod method, int? maxPoints = null)
    {
        if (Type == ProgramContentType.Survey && (method != GradingMethod.None || maxPoints.HasValue))
        {
            throw new InvalidOperationException("Surveys cannot be graded.");
        }

        if (IsLessonType(Type) && (method != GradingMethod.None || maxPoints.HasValue))
        {
            throw new InvalidOperationException("Lessons cannot be graded. Create or attach an assignment instead.");
        }

        GradingMethod = method;
        MaxPoints = maxPoints;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Enforces the persisted learning-content contract after mapping or direct construction.
    /// </summary>
    public void NormalizeLearningContract()
    {
        Type = Type switch
        {
            ProgramContentType.Page => ProgramContentType.Lesson,
            ProgramContentType.Challenge => ProgramContentType.Assignment,
            _ => Type,
        };

        // Defense-in-depth: if both slots are set, clear the one that the routing below
        // would also clear, so the type-specific routing is idempotent regardless of input.
        RouteBodyByContentType();

        if (Type == ProgramContentType.Lesson)
        {
            LessonFormat ??= LessonContentFormatInference.FromBody(Body);
            if (!Enum.IsDefined(LessonFormat.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(LessonFormat),
                    LessonFormat,
                    "Lesson format is not supported.");
            }

            RouteBodyByContentType();
            GradingMethod = GradingMethod.None;
            MaxPoints = null;
            return;
        }

        LessonFormat = null;
        RouteBodyByContentType();

        if (Type == ProgramContentType.Survey)
        {
            GradingMethod = GradingMethod.None;
            MaxPoints = null;
        }

        if (LearningActivityContract.IsActivityType(Type))
        {
            SetActivitySettings(GetActivitySettings()!);
        }
        else
        {
            ActivitySettingsData = null;
        }
    }

    /// <summary>
    /// Routes content between <see cref="Body"/> (text) and <see cref="JsonBody"/> (structured)
    /// by content type and, for lessons, by <see cref="LessonFormat"/>. Enforces mutual exclusion.
    /// </summary>
    private void RouteBodyByContentType()
    {
        if (Type == ProgramContentType.Lesson)
        {
            // LessonFormat may not yet be inferred on the first call; defer to the post-inference call.
            if (!LessonFormat.HasValue)
            {
                return;
            }

            if (LessonFormat == LessonContentFormat.Lexical)
            {
                Body = null;
            }
            else
            {
                JsonBody = null;
            }
            return;
        }

        if (Type is ProgramContentType.Questionnaire
                or ProgramContentType.Code
                or ProgramContentType.Project)
        {
            Body = null;
        }
        else
        {
            JsonBody = null;
        }
    }

    public ActivitySettings? GetActivitySettings() => LearningActivityContract.GetSettings(Type, ActivitySettingsData);

    public void SetActivitySettings(ActivitySettings settings)
    {
        ActivitySettingsData = LearningActivityContract.SerializeSettings(Type, settings);
        UpdatedAt = SystemClock.UtcNow;
    }

    private static bool IsLessonType(ProgramContentType type) =>
        type is ProgramContentType.Lesson or ProgramContentType.Page;

    /// <summary>
    /// Updates estimated completion time
    /// </summary>
    public void UpdateEstimatedTime(int minutes)
    {
        EstimatedMinutes = minutes;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Checks if content is accessible by a specific user
    /// </summary>
    public bool IsAccessibleBy(Guid userId)
    {
        if (Visibility == Visibility.Public)
            return true;

        if (Visibility != Visibility.Internal || Program?.ProgramUsers == null)
            return false;

        return Program.ProgramUsers.Any(pu => pu.UserId == userId && pu.IsActive == true);
    }

    /// <summary>
    /// Calculates completion percentage for a user
    /// </summary>
    public decimal GetCompletionPercentage(Guid userId)
    {
        if (ContentInteractions == null)
            return 0m;

        var interactions = ContentInteractions.Where(ci => ci.UserId == userId).ToList();
        if (interactions.Count == 0)
            return 0m;

        if (Children is { Count: > 0 })
        {
            // For parent content, calculate based on children
            var childCompletions = Children.Select(child => child.GetCompletionPercentage(userId)).ToList();
            return childCompletions.Average();
        }

        // For leaf content, check if completed
        var latestInteraction = interactions.OrderByDescending(i => i.UpdatedAt).First();
        return latestInteraction.IsCompleted ? 100m : (latestInteraction.ProgressPercentage ?? 0m);
    }
}
