using System.ComponentModel.DataAnnotations;

namespace GameGuild.Modules.UserProfile.Dtos;

public class CreateUserProfileDto
{
    private string? _givenName;

    private string? _familyName;

    private string? _displayName;

    private string? _title;

    private string? _description;

    private string? _slug;

    private Guid? _tenantId;

    [StringLength(100)]
    public string? GivenName
    {
        get => _givenName;
        set => _givenName = value;
    }

    [StringLength(100)]
    public string? FamilyName
    {
        get => _familyName;
        set => _familyName = value;
    }

    [StringLength(100)]
    public string? DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    [StringLength(200)]
    public string? Title
    {
        get => _title;
        set => _title = value;
    }

    [StringLength(1000)]
    public string? Description
    {
        get => _description;
        set => _description = value;
    }

    [StringLength(100)]
    public string? Slug
    {
        get => _slug;
        set => _slug = value;
    }

    public Guid? TenantId
    {
        get => _tenantId;
        set => _tenantId = value;
    }
}

public class UpdateUserProfileDto
{
    private string? _givenName;

    private string? _familyName;

    private string? _displayName;

    private string? _title;

    private string? _description;

    private string? _slug;

    private Guid? _tenantId;

    [StringLength(100)]
    public string? GivenName
    {
        get => _givenName;
        set => _givenName = value;
    }

    [StringLength(100)]
    public string? FamilyName
    {
        get => _familyName;
        set => _familyName = value;
    }

    [StringLength(100)]
    public string? DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    [StringLength(200)]
    public string? Title
    {
        get => _title;
        set => _title = value;
    }

    [StringLength(1000)]
    public string? Description
    {
        get => _description;
        set => _description = value;
    }

    [StringLength(100)]
    public string? Slug
    {
        get => _slug;
        set => _slug = value;
    }

    public Guid? TenantId
    {
        get => _tenantId;
        set => _tenantId = value;
    }
}

public class UserProfileResponseDto
{
    private Guid _id;

    private int _version;

    private string? _givenName;

    private string? _familyName;

    private string? _displayName;

    private string? _title;

    private string? _description;

    private string? _slug;

    private Guid? _tenantId;

    private Guid? _createdBy;

    private DateTime _createdAt;

    private DateTime _updatedAt;

    private DateTime? _deletedAt;

    private bool _isDeleted;

    public Guid Id
    {
        get => _id;
        set => _id = value;
    }

    public int Version
    {
        get => _version;
        set => _version = value;
    }

    public string? GivenName
    {
        get => _givenName;
        set => _givenName = value;
    }

    public string? FamilyName
    {
        get => _familyName;
        set => _familyName = value;
    }

    public string? DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    public string? Title
    {
        get => _title;
        set => _title = value;
    }

    public string? Description
    {
        get => _description;
        set => _description = value;
    }

    public string? Slug
    {
        get => _slug;
        set => _slug = value;
    }

    public Guid? TenantId
    {
        get => _tenantId;
        set => _tenantId = value;
    }

    public Guid? CreatedBy
    {
        get => _createdBy;
        set => _createdBy = value;
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = value;
    }

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => _updatedAt = value;
    }

    public DateTime? DeletedAt
    {
        get => _deletedAt;
        set => _deletedAt = value;
    }

    public bool IsDeleted
    {
        get => _isDeleted;
        set => _isDeleted = value;
    }
}
