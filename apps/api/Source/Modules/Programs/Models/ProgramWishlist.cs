using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Program.Models;

[Table("program_wishlists")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(AddedAt))]
public class ProgramWishlist : BaseEntity {
    private Guid _userId;
    private Guid _programId;
    private DateTime _addedAt = DateTime.UtcNow;
    private string? _notes;
    private User.Models.User _user = null!;
    private Program _program = null!;

    public Guid UserId {
        get => _userId;
        set => _userId = value;
    }

    public Guid ProgramId {
        get => _programId;
        set => _programId = value;
    }

    /// <summary>
    /// When the program was added to wishlist
    /// </summary>
    public DateTime AddedAt {
        get => _addedAt;
        set => _addedAt = value;
    }

    /// <summary>
    /// Optional notes about why the user saved this program
    /// </summary>
    [Column(TypeName = "text")]
    public string? Notes {
        get => _notes;
        set => _notes = value;
    }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User.Models.User User {
        get => _user;
        set => _user = value;
    }

    [ForeignKey(nameof(ProgramId))]
    public virtual Program Program {
        get => _program;
        set => _program = value;
    }
}

/// <summary>
/// Entity Framework configuration for ProgramWishlist entity
/// </summary>
public class ProgramWishlistConfiguration : IEntityTypeConfiguration<ProgramWishlist> {
    public void Configure(EntityTypeBuilder<ProgramWishlist> builder) {
        // Additional configuration if needed
    }
}
