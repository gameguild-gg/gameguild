using System.ComponentModel.DataAnnotations;

namespace GameGuild.Assets;

/// <summary>An immutable content snapshot in a logical asset reference history.</summary>
public sealed class AssetReferenceRevision : EntityBase
{
    private AssetReferenceRevision() { }

    public Guid AssetReferenceId { get; private set; }
    public Guid AssetContentId { get; private set; }
    public int RevisionNumber { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    [MaxLength(500)] public string? Note { get; private set; }
    public AssetReference Reference { get; private set; } = null!;
    public AssetContent Content { get; private set; } = null!;

    internal static AssetReferenceRevision Create(
        AssetReference reference,
        Guid contentId,
        int revisionNumber,
        Guid userId,
        string? note) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = reference.TenantId,
            AssetReferenceId = reference.Id,
            AssetContentId = contentId,
            RevisionNumber = revisionNumber,
            CreatedByUserId = userId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
}
