using GameGuild.Identity.Authorization.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>Strongly typed permissions for team operations.</summary>
public sealed class TeamPermission : Permission
{
    private TeamPermission(string key, string description)
        : base(
            resource: "team",
            action: key["team:".Length..],
            scope: null,
            description: description)
    {
    }

    public static class Keys
    {
        public const string Read = "team:read";
        public const string Write = "team:write";
        public const string Admin = "team:admin";
    }

    public static readonly TeamPermission Read = new(Keys.Read, "Read Teams");
    public static readonly TeamPermission Write = new(Keys.Write, "Write Teams");
    public static readonly TeamPermission Admin = new(Keys.Admin, "Administer Teams");
}
