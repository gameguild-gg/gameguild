using GameGuild.Domain.Common;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.TestingLab.Entities;

/// <summary>
/// Represents a registration for a testing session
/// </summary>
[Table("session_registrations")]
[Index(nameof(SessionId))]
[Index(nameof(UserId))]
[Index(nameof(RegistrationType))]
[Index(nameof(Status))]
[Index(nameof(RegisteredAt))]
[Index(nameof(TenantId))]
public class SessionRegistration : EntityBase
{
    /// <summary>
    /// Foreign key to the testing session
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Navigation property to the testing session
    /// </summary>
    public virtual TestingSession Session { get; set; } = null!;

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Type of registration
    /// </summary>
    public RegistrationType RegistrationType { get; set; } = RegistrationType.Tester;

    /// <summary>
    /// Registration status
    /// </summary>
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;

    /// <summary>
    /// When the registration was made
    /// </summary>
    [Required]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user confirmed attendance
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// When the user checked in
    /// </summary>
    public DateTime? CheckedInAt { get; set; }

    /// <summary>
    /// When the user checked out
    /// </summary>
    public DateTime? CheckedOutAt { get; set; }

    /// <summary>
    /// Attendance status
    /// </summary>
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.Registered;

    /// <summary>
    /// Notes about the registration
    /// </summary>
    public string? Notes { get; set; }

    // Computed Properties
    /// <summary>
    /// Whether this registration is global (tenant-independent)
    /// </summary>
    public bool IsGlobal => TenantId == null;

    /// <summary>
    /// Whether the user has confirmed attendance
    /// </summary>
    public bool IsConfirmed => Status == RegistrationStatus.Confirmed;

    /// <summary>
    /// Whether the user has checked in
    /// </summary>
    public bool IsCheckedIn => CheckedInAt.HasValue;

    /// <summary>
    /// Whether the user has checked out
    /// </summary>
    public bool IsCheckedOut => CheckedOutAt.HasValue;

    /// <summary>
    /// Duration of attendance
    /// </summary>
    public TimeSpan? AttendanceDuration => CheckedInAt.HasValue && CheckedOutAt.HasValue
        ? CheckedOutAt.Value - CheckedInAt.Value
        : null;

    // Domain Methods
    /// <summary>
    /// Confirms the registration
    /// </summary>
    public void Confirm()
    {
        Status = RegistrationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the registration
    /// </summary>
    public void Cancel()
    {
        Status = RegistrationStatus.Cancelled;
        AttendanceStatus = AttendanceStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks the user in
    /// </summary>
    public void CheckIn()
    {
        CheckedInAt = DateTime.UtcNow;
        AttendanceStatus = AttendanceStatus.Present;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks the user out
    /// </summary>
    public void CheckOut()
    {
        CheckedOutAt = DateTime.UtcNow;
        AttendanceStatus = AttendanceStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks as no-show
    /// </summary>
    public void MarkNoShow()
    {
        AttendanceStatus = AttendanceStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Registration type enumeration
/// </summary>
public enum RegistrationType
{
    Tester = 0,
    ProjectMember = 1,
    Observer = 2,
    Moderator = 3
}

/// <summary>
/// Registration status enumeration
/// </summary>
public enum RegistrationStatus
{
    Registered = 0,
    Confirmed = 1,
    Cancelled = 2,
    WaitListed = 3
}

/// <summary>
/// Attendance status enumeration
/// </summary>
public enum AttendanceStatus
{
    Registered = 0,
    Present = 1,
    Completed = 2,
    NoShow = 3,
    Late = 4
}