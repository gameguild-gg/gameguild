namespace GameGuild.Assets.Security;

/// <summary>
/// Strongly-typed permission constants for Assets module.
/// Use these constants instead of magic strings in authorization checks.
/// </summary>
public static class AssetsPermission
{
    /// <summary>
    /// Permission key constants for authorization checks.
    /// </summary>
    public static class Keys
    {
        /// <summary>Permission to read/download assets</summary>
        public const string Read = "assets:read";

        /// <summary>Permission to upload new assets</summary>
        public const string Create = "assets:create";

        /// <summary>Permission to update asset metadata</summary>
        public const string Update = "assets:update";

        /// <summary>Permission to delete assets</summary>
        public const string Delete = "assets:delete";

        /// <summary>Permission for admin operations (GC, undeletable marks)</summary>
        public const string Admin = "assets:admin";

        /// <summary>Permission to moderate content</summary>
        public const string Moderate = "assets:moderate";

        /// <summary>Permission to apply transformations</summary>
        public const string Transform = "assets:transform";

        /// <summary>Permission to generate access URLs</summary>
        public const string GenerateUrl = "assets:generate-url";

        /// <summary>Permission to report assets for moderation</summary>
        public const string Report = "assets:report";
    }

    /// <summary>
    /// All available permission keys.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        Keys.Read,
        Keys.Create,
        Keys.Update,
        Keys.Delete,
        Keys.Admin,
        Keys.Moderate,
        Keys.Transform,
        Keys.GenerateUrl,
        Keys.Report
    };

    /// <summary>
    /// Default permissions for asset owners.
    /// </summary>
    public static readonly IReadOnlyList<string> OwnerDefaults = new[]
    {
        Keys.Read,
        Keys.Update,
        Keys.Delete,
        Keys.Transform,
        Keys.GenerateUrl
    };

    /// <summary>
    /// Default permissions for tenant members.
    /// </summary>
    public static readonly IReadOnlyList<string> MemberDefaults = new[]
    {
        Keys.Read,
        Keys.Create,
        Keys.Report
    };

    /// <summary>
    /// Admin-only permissions.
    /// </summary>
    public static readonly IReadOnlyList<string> AdminOnly = new[]
    {
        Keys.Admin,
        Keys.Moderate
    };
}
