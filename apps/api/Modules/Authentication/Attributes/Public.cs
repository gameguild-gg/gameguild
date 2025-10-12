namespace GameGuild.Modules.Authentication;

/// <summary> Attribute to mark endpoints as public (bypasses authentication) </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class Public(bool isPublic = true) : Attribute
{
    public bool IsPublic { get; } = isPublic;
}
