using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Represents a user's rating and review for a program
/// </summary>
[Table("program_ratings")]
[Index(nameof(ProgramId))]
[Index(nameof(UserId))]
[Index(nameof(Rating))]
[Index(nameof(CreatedAt))]
public class ProgramRating : Entity
{
    /// <summary>
    /// The ID of the program being rated
    /// </summary>
    public Guid ProgramId { get; set; }

    /// <summary>
    /// The ID of the user providing the rating
    /// </summary>
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The numerical rating (1-5 scale)
    /// </summary>
    [Range(1, 5)]
    public decimal Rating { get; set; }

    /// <summary>
    /// Optional written review
    /// </summary>
    [MaxLength(2000)]
    public string? Review { get; set; }

    /// <summary>
    /// Whether this rating is verified (user completed the program)
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Whether this rating is featured/highlighted
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Number of helpful votes this review received
    /// </summary>
    public int HelpfulVotes { get; set; } = 0;

    /// <summary>
    /// Number of unhelpful votes this review received
    /// </summary>
    public int UnhelpfulVotes { get; set; } = 0;

    // Navigation properties
    public virtual Program Program { get; set; } = null!;
    
    // Note: User navigation property would be added when User model is available
    // public virtual User User { get; set; } = null!;
}
