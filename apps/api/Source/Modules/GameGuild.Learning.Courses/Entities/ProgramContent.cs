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
    /// Content body/text
    /// </summary>
    public string? Body { get; set; }

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
        GradingMethod = method;
        MaxPoints = maxPoints;
        UpdatedAt = SystemClock.UtcNow;
    }

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
