namespace GameGuild.Identity.Users;

/// <summary>
///     Result of user authentication validation.
/// </summary>
public sealed record UserAuthenticationResult
{
    private UserAuthenticationResult(bool isSuccess, UserAuthenticationFailure? failureReason = null)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
    }

    /// <summary>
    ///     Whether authentication validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     The reason for failure, if any.
    /// </summary>
    public UserAuthenticationFailure? FailureReason { get; }

    /// <summary>
    ///     Creates a successful authentication result.
    /// </summary>
    public static UserAuthenticationResult Success() => new(true);

    /// <summary>
    ///     Creates a failed authentication result.
    /// </summary>
    public static UserAuthenticationResult Fail(UserAuthenticationFailure failure) => new(false, failure);
}

/// <summary>
///     Reasons why authentication validation can fail.
/// </summary>
public enum UserAuthenticationFailure
{
    /// <summary>User account is inactive.</summary>
    Inactive,

    /// <summary>User account is suspended.</summary>
    Suspended,

    /// <summary>Token version mismatch - token has been revoked.</summary>
    TokenRevoked
}

/// <summary>
///     Result of user registration validation.
/// </summary>
public sealed record UserRegistrationResult
{
    private UserRegistrationResult(bool isSuccess, IReadOnlyList<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>
    ///     Whether registration validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    ///     Creates a successful registration result.
    /// </summary>
    public static UserRegistrationResult Success() => new(true);

    /// <summary>
    ///     Creates a failed registration result.
    /// </summary>
    public static UserRegistrationResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}

/// <summary>
///     Result of tenant join validation.
/// </summary>
public sealed record UserTenantJoinResult
{
    private UserTenantJoinResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    ///     Whether tenant join validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     The error message, if any.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static UserTenantJoinResult Success() => new(true);

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    public static UserTenantJoinResult Failure(string error) => new(false, error);
}
