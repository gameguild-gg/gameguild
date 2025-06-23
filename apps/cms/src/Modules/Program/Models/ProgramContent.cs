using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;

namespace GameGuild.Modules.Program.Models;

[Table("program_contents")]
[Index(nameof(ProgramId))]
[Index(nameof(ParentId))]
[Index(nameof(Type))]
[Index(nameof(Visibility))]
[Index(nameof(SortOrder))]
[Index(nameof(ProgramId), nameof(SortOrder))]
[Index(nameof(ParentId), nameof(SortOrder))]
[Index(nameof(IsRequired))]
public class ProgramContent : BaseEntity
{
    private Guid _programId;

    private Guid? _parentId;

    private string _title = string.Empty;

    private string _description = string.Empty;

    private ProgramContentType _type;

    private string _body = "{}";

    private int _sortOrder = 0;

    private bool _isRequired = true;

    private GradingMethod? _gradingMethod;

    private decimal? _maxPoints;

    private int? _estimatedMinutes;

    private Visibility _visibility = GameGuild.Common.Enums.Visibility.Published;

    private Program _program = null!;

    private ProgramContent? _parent;

    private ICollection<ProgramContent> _children = new List<ProgramContent>();

    private ICollection<ContentInteraction> _contentInteractions = new List<ContentInteraction>();

    [Required]
    [ForeignKey(nameof(Program))]
    public Guid ProgramId
    {
        get => _programId;
        set => _programId = value;
    }

    /// <summary>
    /// For hierarchical content structure (e.g., modules containing lessons)
    /// </summary>
    [ForeignKey(nameof(Parent))]
    public Guid? ParentId
    {
        get => _parentId;
        set => _parentId = value;
    }

    [Required]
    [MaxLength(255)]
    public string Title
    {
        get => _title;
        set => _title = value;
    }

    public string Description
    {
        get => _description;
        set => _description = value;
    }

    public ProgramContentType Type
    {
        get => _type;
        set => _type = value;
    }

    /// <summary>
    /// Main content body stored as JSON to support rich content
    /// Structure varies by content type:
    /// - Page: {content: "HTML/Markdown", resources: []}
    /// - Assignment: {instructions: "", rubric: {}, submissionFormat: ""}
    /// - Code: {starterCode: "", testCases: [], language: ""}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Body
    {
        get => _body;
        set => _body = value;
    }

    /// <summary>
    /// Display order within the program or parent content
    /// </summary>
    public int SortOrder
    {
        get => _sortOrder;
        set => _sortOrder = value;
    }

    /// <summary>
    /// Whether this content is required for program completion
    /// </summary>
    public bool IsRequired
    {
        get => _isRequired;
        set => _isRequired = value;
    }

    /// <summary>
    /// How this content should be graded (if applicable)
    /// </summary>
    public GradingMethod? GradingMethod
    {
        get => _gradingMethod;
        set => _gradingMethod = value;
    }

    /// <summary>
    /// Maximum points/score for this content (if gradeable)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxPoints
    {
        get => _maxPoints;
        set => _maxPoints = value;
    }

    /// <summary>
    /// Estimated time to complete in minutes
    /// </summary>
    public int? EstimatedMinutes
    {
        get => _estimatedMinutes;
        set => _estimatedMinutes = value;
    }

    public Common.Enums.Visibility Visibility
    {
        get => _visibility;
        set => _visibility = value;
    }

    // Navigation properties
    public virtual Program Program
    {
        get => _program;
        set => _program = value;
    }

    public virtual ProgramContent? Parent
    {
        get => _parent;
        set => _parent = value;
    }

    public virtual ICollection<ProgramContent> Children
    {
        get => _children;
        set => _children = value;
    }

    public virtual ICollection<ContentInteraction> ContentInteractions
    {
        get => _contentInteractions;
        set => _contentInteractions = value;
    }

    // Helper methods for JSON body
    public T? GetBodyContent<T>(string key) where T : class
    {
        if (string.IsNullOrEmpty(Body)) return null;

        try
        {
            JsonDocument json = JsonDocument.Parse(Body);
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

    public void SetBodyContent<T>(string key, T value)
    {
        var body = string.IsNullOrEmpty(Body) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(Body) ?? new Dictionary<string, object>();

        body[key] = value!;
        Body = JsonSerializer.Serialize(body);
    }

    public string GetContent()
    {
        return GetBodyContent<string>("content") ?? string.Empty;
    }

    public void SetContent(string content)
    {
        SetBodyContent("content", content);
    }
}

/// <summary>
/// Entity Framework configuration for ProgramContent entity
/// </summary>
public class ProgramContentConfiguration : IEntityTypeConfiguration<ProgramContent>
{
    public void Configure(EntityTypeBuilder<ProgramContent> builder)
    {
        // Configure relationship with Program (can't be done with annotations)
        builder.HasOne(pc => pc.Program)
            .WithMany(p => p.ProgramContents)
            .HasForeignKey(pc => pc.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with Parent (self-referencing, can't be done with annotations)
        builder.HasOne(pc => pc.Parent)
            .WithMany(pc => pc.Children)
            .HasForeignKey(pc => pc.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
