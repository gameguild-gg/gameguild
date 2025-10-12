namespace GameGuild.Core.Domain.Identity;

/// <summary> Context interface for resource identification within a request scope </summary>
public interface IResourceContext
{
    /// <summary> Gets the current resource ID if available </summary>
    Guid? ResourceId { get; }

    /// <summary> Gets the current resource type if available </summary>
    string? ResourceType { get; }

    /// <summary> Gets a string identifier for the current resource </summary>
    /// <returns> Resource identifier string </returns>
    string GetResourceIdentifier();

    /// <summary> Sets the current resource context </summary>
    /// <param name="resourceType"> The resource type </param>
    /// <param name="resourceId"> The resource ID </param>
    void SetResource(string resourceType, Guid resourceId);

    /// <summary> Clears the current resource context </summary>
    void ClearResource();
}
