using GameGuild.Identity.Users;


namespace GameGuild.TestingLab;

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
    public DateTime RegisteredAt { get; set; } = SystemClock.UtcNow;

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

    public string? RegistrationNotes {
        get => Notes;
        set => Notes = value;
    }

    public DateTime? AttendedAt {
        get => CheckedInAt;
        set => CheckedInAt = value;
    }

    // Computed Properties
    /// <summary>
    /// Whether this registration is global (tenant-independent)
    /// </summary>
    public override bool IsGlobal => TenantId == null;

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
        ConfirmedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Cancels the registration
    /// </summary>
    public void Cancel()
    {
        Status = RegistrationStatus.Cancelled;
        AttendanceStatus = AttendanceStatus.NoShow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Checks the user in
    /// </summary>
    public void CheckIn()
    {
        CheckedInAt = SystemClock.UtcNow;
        AttendanceStatus = AttendanceStatus.Present;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Checks the user out
    /// </summary>
    public void CheckOut()
    {
        CheckedOutAt = SystemClock.UtcNow;
        AttendanceStatus = AttendanceStatus.Completed;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Marks as no-show
    /// </summary>
    public void MarkNoShow()
    {
        AttendanceStatus = AttendanceStatus.NoShow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Updates notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = SystemClock.UtcNow;
    }
}
