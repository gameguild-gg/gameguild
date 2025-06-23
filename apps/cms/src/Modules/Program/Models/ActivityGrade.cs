using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Program.Models;

[Table("activity_grades")]
public class ActivityGrade : BaseEntity
{
    private Guid _contentInteractionId;

    private Guid _graderProgramUserId;

    private decimal _grade;

    private string? _feedback;

    private string? _gradingDetails;

    private DateTime _gradedAt;

    private ContentInteraction _contentInteraction = null!;

    private ProgramUser _graderProgramUser = null!;

    public Guid ContentInteractionId
    {
        get => _contentInteractionId;
        set => _contentInteractionId = value;
    }

    public Guid GraderProgramUserId
    {
        get => _graderProgramUserId;
        set => _graderProgramUserId = value;
    }

    /// <summary>
    /// Grade awarded (0-100 scale or points based on content max_points)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Grade
    {
        get => _grade;
        set => _grade = value;
    }

    /// <summary>
    /// Written feedback from the grader
    /// </summary>
    public string? Feedback
    {
        get => _feedback;
        set => _feedback = value;
    }

    /// <summary>
    /// Detailed grading breakdown stored as JSON
    /// Examples: rubric scores, test case results, peer review criteria
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? GradingDetails
    {
        get => _gradingDetails;
        set => _gradingDetails = value;
    }

    /// <summary>
    /// Date when the grade was awarded
    /// </summary>
    public DateTime GradedAt
    {
        get => _gradedAt;
        set => _gradedAt = value;
    }

    // Navigation properties
    public virtual ContentInteraction ContentInteraction
    {
        get => _contentInteraction;
        set => _contentInteraction = value;
    }

    public virtual ProgramUser GraderProgramUser
    {
        get => _graderProgramUser;
        set => _graderProgramUser = value;
    }

    // Helper methods for JSON grading details
    public T? GetGradingDetail<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(GradingDetails)) return null;

        try
        {
            JsonDocument json = JsonDocument.Parse(GradingDetails);
            if (json.RootElement.TryGetProperty(key, out JsonElement element))
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
        }
        catch
        {
            // Handle JSON parsing errors gracefully
        }

        return null;
    }

    public void SetGradingDetail<T>(string key, T value)
    {
        var details = string.IsNullOrEmpty(GradingDetails) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(GradingDetails) ?? new Dictionary<string, object>();

        details[key] = value!;
        GradingDetails = JsonSerializer.Serialize(details);
    }
}

/// <summary>
/// Entity Framework configuration for ActivityGrade entity
/// </summary>
public class ActivityGradeConfiguration : IEntityTypeConfiguration<ActivityGrade>
{
    public void Configure(EntityTypeBuilder<ActivityGrade> builder)
    {
        // Configure relationship with ContentInteraction (can't be done with annotations)
        builder.HasOne(ag => ag.ContentInteraction)
            .WithMany(ci => ci.ActivityGrades)
            .HasForeignKey(ag => ag.ContentInteractionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with GraderProgramUser (can't be done with annotations)
        builder.HasOne(ag => ag.GraderProgramUser)
            .WithMany()
            .HasForeignKey(ag => ag.GraderProgramUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
