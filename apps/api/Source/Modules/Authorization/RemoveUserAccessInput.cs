namespace GameGuild.GraphQL;

/// <summary> GraphQL input for removing user access </summary>
public record RemoveUserAccessInput(string ResourceType, Guid ResourceId, Guid TargetUserId);