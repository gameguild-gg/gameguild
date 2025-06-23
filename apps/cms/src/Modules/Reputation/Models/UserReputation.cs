using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Reputation.Models;

/// <summary>
/// Tracks a user's reputation score and tier
/// </summary>
[Table("UserReputations")]
[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(Score))]
[Index(nameof(CurrentLevelId))]
public class UserReputation : ResourceBase, IReputation
{
    private User.Models.User _user;

    private Guid _userId;

    private int _score = 0;

    private ReputationTier? _currentLevel;

    private Guid? _currentLevelId;

    private DateTime _lastUpdated = DateTime.UtcNow;

    private DateTime? _lastLevelCalculation;

    private int _positiveChanges = 0;

    private int _negativeChanges = 0;

    private ICollection<UserReputationHistory> _history = new List<UserReputationHistory>();

    /// <summary>
    /// The user this reputation belongs to
    /// </summary>
    [Required]
    [ForeignKey(nameof(UserId))]
    public required Modules.User.Models.User User
    {
        get => _user;
        [MemberNotNull(nameof(_user))] set => _user = value;
    }

    [Required]
    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    /// <summary>
    /// Current reputation score
    /// </summary>
    public int Score
    {
        get => _score;
        set => _score = value;
    }

    /// <summary>
    /// Current reputation tier (linked to configurable tier)
    /// </summary>
    [ForeignKey(nameof(CurrentLevelId))]
    public ReputationTier? CurrentLevel
    {
        get => _currentLevel;
        set => _currentLevel = value;
    }

    public Guid? CurrentLevelId
    {
        get => _currentLevelId;
        set => _currentLevelId = value;
    }

    /// <summary>
    /// When the reputation was last updated
    /// </summary>
    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set => _lastUpdated = value;
    }

    /// <summary>
    /// When the user's reputation tier was last recalculated
    /// </summary>
    public DateTime? LastLevelCalculation
    {
        get => _lastLevelCalculation;
        set => _lastLevelCalculation = value;
    }

    /// <summary>
    /// Number of positive reputation changes
    /// </summary>
    public int PositiveChanges
    {
        get => _positiveChanges;
        set => _positiveChanges = value;
    }

    /// <summary>
    /// Number of negative reputation changes
    /// </summary>
    public int NegativeChanges
    {
        get => _negativeChanges;
        set => _negativeChanges = value;
    }

    /// <summary>
    /// History of reputation changes for this user
    /// </summary>
    public ICollection<UserReputationHistory> History
    {
        get => _history;
        set => _history = value;
    }
}

public class UserReputationConfiguration : IEntityTypeConfiguration<UserReputation>
{
    public void Configure(EntityTypeBuilder<UserReputation> builder)
    {
        // Configure relationship with User (can't be done with annotations)
        builder.HasOne(ur => ur.User)
            .WithMany()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with CurrentLevel (can't be done with annotations)
        builder.HasOne(ur => ur.CurrentLevel)
            .WithMany()
            .HasForeignKey(ur => ur.CurrentLevelId)
            .OnDelete(DeleteBehavior.SetNull);

        // Filtered unique constraint (can't be done with annotations)
        builder.HasIndex(ur => ur.UserId)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}
