namespace GameGuild.Identity.Authentication;

/// <summary>
///     Base interface for credential data
/// </summary>
public interface ICredentialData
{
    string Type { get; }
}
