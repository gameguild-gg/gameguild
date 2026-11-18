using GameGuild.Identity.Authorization.Models;

namespace GameGuild.Gamification.Achievements;

/// <summary>
///     Typed permissions for the Achievements admin surface (issue #1319 G2). Previously these
///     four keys were plain string constants: the
///     <see cref="GameGuild.Identity.Authorization.PermissionRegistry"/> never
///     discovered them, no grant could be validated, and the admin mutations returned 403 for
///     every caller. As a <see cref="Permission"/> subclass the registry discovers the keys,
///     grants validate, and the administrative tenant roles receive them through the
///     role-permission providers.
/// </summary>
public sealed class AchievementsPermission : Permission
{
    private AchievementsPermission(string key, string description)
        : base(
            resource: key.Split(':')[0],
            action: key.Split(':')[1],
            scope: key.Split(':').Length > 2 ? key.Split(':')[2] : null,
            description: description)
    {
    }

    /// <summary>
    ///     Permission key constants for use in attributes and grants.
    /// </summary>
    public static class Keys
    {
        /// <summary>Create new achievement definitions</summary>
        public const string Create = "achievements:create";

        /// <summary>Update existing achievement definitions</summary>
        public const string Update = "achievements:update";

        /// <summary>Soft-delete achievement definitions</summary>
        public const string Delete = "achievements:delete";

        /// <summary>Award achievements to users manually</summary>
        public const string Award = "achievements:award";
    }

    /// <summary>Create new achievement definitions</summary>
    public static readonly AchievementsPermission Create = new(Keys.Create, "Create achievement definitions");

    /// <summary>Update existing achievement definitions</summary>
    public static readonly AchievementsPermission Update = new(Keys.Update, "Update achievement definitions");

    /// <summary>Soft-delete achievement definitions</summary>
    public static readonly AchievementsPermission Delete = new(Keys.Delete, "Delete achievement definitions");

    /// <summary>Award achievements to users manually</summary>
    public static readonly AchievementsPermission Award = new(Keys.Award, "Manually award achievements to users");
}
