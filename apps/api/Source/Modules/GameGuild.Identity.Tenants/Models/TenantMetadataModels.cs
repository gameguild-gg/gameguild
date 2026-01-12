namespace GameGuild.Identity.Tenants;

/// <summary>
///     Data transfer object representing tenant metadata information
/// </summary>
/// <param name="Id">Unique identifier for the tenant</param>
/// <param name="CustomFields">Dictionary of custom metadata fields specific to the tenant</param>
/// <param name="Tags">Collection of tags for categorization and filtering</param>
/// <param name="ExternalReferences">External system identifiers and references</param>
/// <param name="BusinessInfo">Business classification and organizational information</param>
/// <param name="ContactInfo">Contact details and organizational data</param>
/// <param name="AdminNotes">Administrative notes and documentation</param>
/// <param name="CreatedAt">Timestamp when the metadata was created</param>
/// <param name="UpdatedAt">Timestamp when the metadata was last updated</param>
public record TenantMetadataDto(
    Guid Id,
    Dictionary<string, object?> CustomFields,
    List<string> Tags,
    Dictionary<string, string> ExternalReferences,
    TenantBusinessInfoDto BusinessInfo,
    TenantContactInfoDto ContactInfo,
    string? AdminNotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>
///     Business information data transfer object for tenant classification
/// </summary>
/// <param name="Industry">Industry sector and vertical market</param>
/// <param name="OrganizationSize">Organization size classification</param>
/// <param name="TenantType">Type and category of the tenant</param>
/// <param name="GeographicRegion">Geographic location and regulatory information</param>
/// <param name="ComplianceRequirements">Compliance and regulatory requirements</param>
public record TenantBusinessInfoDto(string? Industry, string? OrganizationSize, string? TenantType, string? GeographicRegion, List<string> ComplianceRequirements);

/// <summary>
///     Contact information data transfer object for tenant organizational data
/// </summary>
/// <param name="PrimaryContactName">Name of the primary contact person</param>
/// <param name="PrimaryContactEmail">Email address of the primary contact</param>
/// <param name="PrimaryContactPhone">Phone number of the primary contact</param>
/// <param name="OrganizationName">Name of the organization</param>
/// <param name="Address">Physical address of the organization</param>
/// <param name="Website">Website URL of the organization</param>
public record TenantContactInfoDto(string? PrimaryContactName, string? PrimaryContactEmail, string? PrimaryContactPhone, string? OrganizationName, TenantAddressDto? Address, string? Website);

/// <summary>
///     Address information data transfer object
/// </summary>
/// <param name="Street">Street address</param>
/// <param name="City">City name</param>
/// <param name="State">State or province</param>
/// <param name="PostalCode">Postal or ZIP code</param>
/// <param name="Country">Country name</param>
public record TenantAddressDto(string? Street, string? City, string? State, string? PostalCode, string? Country);

/// <summary>
///     Request model for updating tenant metadata
/// </summary>
/// <param name="CustomFields">Custom metadata fields to update (partial update)</param>
/// <param name="Tags">Tags to add or update</param>
/// <param name="ExternalReferences">External references to update</param>
/// <param name="BusinessInfo">Business information to update</param>
/// <param name="ContactInfo">Contact information to update</param>
/// <param name="AdminNotes">Administrative notes to update</param>
public record UpdateTenantMetadataRequest(
    Dictionary<string, object?>? CustomFields,
    List<string>? Tags,
    Dictionary<string, string>? ExternalReferences,
    UpdateTenantBusinessInfoRequest? BusinessInfo,
    UpdateTenantContactInfoRequest? ContactInfo,
    string? AdminNotes
);

/// <summary>
///     Request model for replacing entire tenant metadata
/// </summary>
/// <param name="CustomFields">Complete set of custom metadata fields</param>
/// <param name="Tags">Complete set of tags</param>
/// <param name="ExternalReferences">Complete set of external references</param>
/// <param name="BusinessInfo">Complete business information</param>
/// <param name="ContactInfo">Complete contact information</param>
/// <param name="AdminNotes">Administrative notes</param>
public record ReplaceTenantMetadataRequest(
    Dictionary<string, object?> CustomFields,
    List<string> Tags,
    Dictionary<string, string> ExternalReferences,
    UpdateTenantBusinessInfoRequest BusinessInfo,
    UpdateTenantContactInfoRequest ContactInfo,
    string? AdminNotes
);

/// <summary>
///     Request model for updating tenant business information
/// </summary>
/// <param name="Industry">Industry sector</param>
/// <param name="OrganizationSize">Organization size</param>
/// <param name="TenantType">Tenant type</param>
/// <param name="GeographicRegion">Geographic region</param>
/// <param name="ComplianceRequirements">Compliance requirements</param>
public record UpdateTenantBusinessInfoRequest(string? Industry, string? OrganizationSize, string? TenantType, string? GeographicRegion, List<string>? ComplianceRequirements);

/// <summary>
///     Request model for updating tenant contact information
/// </summary>
/// <param name="PrimaryContactName">Primary contact name</param>
/// <param name="PrimaryContactEmail">Primary contact email</param>
/// <param name="PrimaryContactPhone">Primary contact phone</param>
/// <param name="OrganizationName">Organization name</param>
/// <param name="Address">Organization address</param>
/// <param name="Website">Organization website</param>
public record UpdateTenantContactInfoRequest(string? PrimaryContactName, string? PrimaryContactEmail, string? PrimaryContactPhone, string? OrganizationName, UpdateTenantAddressRequest? Address, string? Website);

/// <summary>
///     Request model for updating tenant address information
/// </summary>
/// <param name="Street">Street address</param>
/// <param name="City">City name</param>
/// <param name="State">State or province</param>
/// <param name="PostalCode">Postal or ZIP code</param>
/// <param name="Country">Country name</param>
public record UpdateTenantAddressRequest(string? Street, string? City, string? State, string? PostalCode, string? Country);

/// <summary>
///     Request model for updating tenant custom fields
/// </summary>
/// <param name="CustomFields">Dictionary of custom fields to update</param>
public record UpdateTenantCustomFieldsRequest(Dictionary<string, object?> CustomFields);

/// <summary>
///     Request model for updating tenant tags
/// </summary>
/// <param name="Tags">List of tags to set</param>
public record UpdateTenantTagsRequest(List<string> Tags);

/// <summary>
///     Request model for replacing tenant tags
/// </summary>
/// <param name="Tags">Complete list of tags to replace existing tags</param>
public record ReplaceTenantTagsRequest(List<string> Tags);
