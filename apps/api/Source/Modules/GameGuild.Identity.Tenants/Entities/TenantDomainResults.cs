namespace GameGuild.Identity.Tenants;

/// <summary>
///     Result of tenant membership validation.
/// </summary>
public sealed record TenantMembershipResult
{
    private TenantMembershipResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    ///     Whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     The error message, if any.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static TenantMembershipResult Success() => new(true);

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static TenantMembershipResult Failure(string error) => new(false, error);
}

/// <summary>
///     Result of tenant configuration validation.
/// </summary>
public sealed record TenantValidationResult
{
    private TenantValidationResult(bool isSuccess, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>
    ///     Whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static TenantValidationResult Success() => new(true);

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static TenantValidationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}

/// <summary>
///     Result of tenant archive validation.
/// </summary>
public sealed record TenantArchiveResult
{
    private TenantArchiveResult(bool isSuccess, int affectedMemberCount = 0, string? error = null)
    {
        IsSuccess = isSuccess;
        AffectedMemberCount = affectedMemberCount;
        Error = error;
    }

    /// <summary>
    ///     Whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Number of active members who will lose access if archived.
    /// </summary>
    public int AffectedMemberCount { get; }

    /// <summary>
    ///     The error message, if any.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static TenantArchiveResult Success(int affectedMemberCount) => new(true, affectedMemberCount);

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static TenantArchiveResult Failure(string error) => new(false, error: error);
}

/// <summary>
///     Result of tenant deletion validation.
/// </summary>
public sealed record TenantDeleteResult
{
    private TenantDeleteResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    ///     Whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     The error message, if any.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static TenantDeleteResult Success() => new(true);

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static TenantDeleteResult Failure(string error) => new(false, error);
}
