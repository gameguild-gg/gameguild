using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;
using GameGuild.Modules.Program.Models;

namespace GameGuild.Modules.Feedback.Models;

[Table("program_ratings")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(ProductId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(Rating))]
[Index(nameof(ModerationStatus))]
[Index(nameof(SubmittedAt))]
public class ProgramRating : BaseEntity
{
    private Guid _userId;

    private Guid _programId;

    private Guid? _productId;

    private Guid _programUserId;

    private decimal _rating;

    private string? _review;

    private decimal? _contentQualityRating;

    private decimal? _instructorRating;

    private decimal? _difficultyRating;

    private decimal? _valueRating;

    private bool? _wouldRecommend;

    private ModerationStatus _moderationStatus = ModerationStatus.Pending;

    private Guid? _moderatedBy;

    private DateTime? _moderatedAt;

    private DateTime _submittedAt;

    private User.Models.User _user = null!;

    private Program.Models.Program _program = null!;

    private Product.Models.Product? _product;

    private ProgramUser _programUser = null!;

    private User.Models.User? _moderator;

    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    public Guid ProgramId
    {
        get => _programId;
        set => _programId = value;
    }

    public Guid? ProductId
    {
        get => _productId;
        set => _productId = value;
    }

    public Guid ProgramUserId
    {
        get => _programUserId;
        set => _programUserId = value;
    }

    /// <summary>
    /// Overall rating for the program (1-5)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal Rating
    {
        get => _rating;
        set => _rating = value;
    }

    /// <summary>
    /// Written review of the program
    /// </summary>
    public string? Review
    {
        get => _review;
        set => _review = value;
    }

    /// <summary>
    /// Rating for content quality (1-5)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal? ContentQualityRating
    {
        get => _contentQualityRating;
        set => _contentQualityRating = value;
    }

    /// <summary>
    /// Rating for instructor effectiveness (1-5)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal? InstructorRating
    {
        get => _instructorRating;
        set => _instructorRating = value;
    }

    /// <summary>
    /// Rating for program difficulty (1-5)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal? DifficultyRating
    {
        get => _difficultyRating;
        set => _difficultyRating = value;
    }

    /// <summary>
    /// Rating for value for money (1-5)
    /// </summary>
    [Column(TypeName = "decimal(2,1)")]
    public decimal? ValueRating
    {
        get => _valueRating;
        set => _valueRating = value;
    }

    /// <summary>
    /// Whether the user would recommend this program
    /// </summary>
    public bool? WouldRecommend
    {
        get => _wouldRecommend;
        set => _wouldRecommend = value;
    }

    public ModerationStatus ModerationStatus
    {
        get => _moderationStatus;
        set => _moderationStatus = value;
    }

    /// <summary>
    /// User who moderated this rating (approved/rejected)
    /// </summary>
    public Guid? ModeratedBy
    {
        get => _moderatedBy;
        set => _moderatedBy = value;
    }

    /// <summary>
    /// Date when rating was moderated
    /// </summary>
    public DateTime? ModeratedAt
    {
        get => _moderatedAt;
        set => _moderatedAt = value;
    }

    /// <summary>
    /// Date when rating was submitted
    /// </summary>
    public DateTime SubmittedAt
    {
        get => _submittedAt;
        set => _submittedAt = value;
    }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User.Models.User User
    {
        get => _user;
        set => _user = value;
    }

    [ForeignKey(nameof(ProgramId))]
    public virtual Program.Models.Program Program
    {
        get => _program;
        set => _program = value;
    }

    [ForeignKey(nameof(ProductId))]
    public virtual Product.Models.Product? Product
    {
        get => _product;
        set => _product = value;
    }

    [ForeignKey(nameof(ProgramUserId))]
    public virtual Program.Models.ProgramUser ProgramUser
    {
        get => _programUser;
        set => _programUser = value;
    }

    [ForeignKey(nameof(ModeratedBy))]
    public virtual User.Models.User? Moderator
    {
        get => _moderator;
        set => _moderator = value;
    }
}

public class ProgramRatingConfiguration : IEntityTypeConfiguration<ProgramRating>
{
    public void Configure(EntityTypeBuilder<ProgramRating> builder)
    {
        // Configure relationship with Program (can't be done with annotations)
        builder.HasOne(pr => pr.Program)
            .WithMany()
            .HasForeignKey(pr => pr.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with User (can't be done with annotations)
        builder.HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
