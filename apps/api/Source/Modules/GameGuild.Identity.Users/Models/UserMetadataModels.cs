namespace GameGuild.Identity.Users;

/// <summary>
///     Data transfer object for user metadata
/// </summary>
/// <param name="Id">The unique identifier for the user metadata</param>
/// <param name="UserId">The user identifier that this metadata belongs to</param>
/// <param name="CustomFields">Dictionary of custom field keys and values</param>
/// <param name="Tags">List of tags associated with the user</param>
/// <param name="ExternalReferences">Dictionary of external system references</param>
/// <param name="CreatedAt">When the metadata was created</param>
/// <param name="UpdatedAt">When the metadata was last updated</param>
/// <param name="Version">Version for optimistic concurrency control</param>
public record UserMetadataDto(
    Guid Id,
    Guid UserId,
    Dictionary<string, object?> CustomFields,
    List<string> Tags,
    Dictionary<string, string> ExternalReferences,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    byte[ ] Version
);

/// <summary>
///     Request model for updating user metadata
/// </summary>
/// <param name="CustomFields">Custom fields to update (only provided keys are modified)</param>
/// <param name="TagsToAdd">Tags to add to the user</param>
/// <param name="TagsToRemove">Tags to remove from the user</param>
/// <param name="ExternalReferences">External references to update</param>
public record UpdateUserMetadataRequest(Dictionary<string, object?>? CustomFields = null, List<string>? TagsToAdd = null, List<string>? TagsToRemove = null, Dictionary<string, string>? ExternalReferences = null);

/// <summary>
///     Request model for completely replacing user metadata
/// </summary>
/// <param name="CustomFields">Complete set of custom fields</param>
/// <param name="Tags">Complete set of tags</param>
/// <param name="ExternalReferences">Complete set of external references</param>
public record ReplaceUserMetadataRequest(Dictionary<string, object?> CustomFields, List<string> Tags, Dictionary<string, string> ExternalReferences);

/// <summary>
///     Request model for updating user custom fields
/// </summary>
/// <param name="CustomFields">Custom fields to update</param>
public record UpdateUserCustomFieldsRequest(Dictionary<string, object?> CustomFields);

/// <summary>
///     Request model for updating user tags
/// </summary>
/// <param name="TagsToAdd">Tags to add</param>
/// <param name="TagsToRemove">Tags to remove</param>
public record UpdateUserTagsRequest(List<string>? TagsToAdd = null, List<string>? TagsToRemove = null);

/// <summary>
///     Request model for replacing all user tags
/// </summary>
/// <param name="Tags">Complete set of tags</param>
public record ReplaceUserTagsRequest(List<string> Tags);
