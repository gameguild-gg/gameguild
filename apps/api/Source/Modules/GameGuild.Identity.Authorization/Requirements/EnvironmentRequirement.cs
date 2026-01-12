using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Requirement that validates ABAC environment constraints.
/// </summary>
public sealed class EnvironmentRequirement : IAuthorizationRequirement
{
    /// <summary>
    ///     Gets the environment constraints to validate.
    /// </summary>
    public EnvironmentConstraints Constraints { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="EnvironmentRequirement"/>.
    /// </summary>
    /// <param name="constraints">The environment constraints.</param>
    public EnvironmentRequirement(EnvironmentConstraints constraints)
    {
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
    }
}
