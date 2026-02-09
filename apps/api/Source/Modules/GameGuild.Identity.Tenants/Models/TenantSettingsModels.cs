namespace GameGuild.Identity.Tenants;

/// <summary>
///     Data transfer object representing tenant settings
/// </summary>
/// <param name="Id">Unique identifier for the tenant</param>
/// <param name="SystemConfiguration">System-wide configuration settings</param>
/// <param name="FeatureFlags">Feature toggles and experimental features</param>
/// <param name="BusinessRules">Business logic and operational rules</param>
/// <param name="UserInterfaceSettings">UI customization and appearance settings</param>
/// <param name="SecuritySettings">Security policies and access controls</param>
/// <param name="IntegrationSettings">Third-party integration configurations</param>
/// <param name="SystemLimits">Usage limits and resource constraints</param>
/// <param name="CreatedAt">Timestamp when the settings were created</param>
/// <param name="UpdatedAt">Timestamp when the settings were last updated</param>
public sealed record TenantSettingsDto(
    Guid Id,
    TenantSystemConfigurationDto SystemConfiguration,
    Dictionary<string, bool> FeatureFlags,
    TenantBusinessRulesDto BusinessRules,
    TenantUiSettingsDto UserInterfaceSettings,
    TenantSecuritySettingsDto SecuritySettings,
    TenantIntegrationSettingsDto IntegrationSettings,
    TenantSystemLimitsDto SystemLimits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>
///     System configuration data transfer object
/// </summary>
/// <param name="TimeZone">Default time zone for the tenant</param>
/// <param name="Locale">Default locale and language settings</param>
/// <param name="DateFormat">Default date format preference</param>
/// <param name="NumberFormat">Default number format preference</param>
/// <param name="CurrencySettings">Currency and monetary settings</param>
/// <param name="CustomConfiguration">Additional custom configuration settings</param>
public sealed record TenantSystemConfigurationDto(string TimeZone, string Locale, string DateFormat, string NumberFormat, TenantCurrencySettingsDto CurrencySettings, Dictionary<string, object?> CustomConfiguration);

/// <summary>
///     Currency settings data transfer object
/// </summary>
/// <param name="DefaultCurrency">Default currency code (ISO 4217)</param>
/// <param name="DisplayFormat">Currency display format</param>
/// <param name="DecimalPlaces">Number of decimal places for currency</param>
public sealed record TenantCurrencySettingsDto(string DefaultCurrency, string DisplayFormat, int DecimalPlaces);

/// <summary>
///     Business rules data transfer object
/// </summary>
/// <param name="WorkflowRules">Workflow and process automation rules</param>
/// <param name="ValidationRules">Data validation and business logic rules</param>
/// <param name="ApprovalRules">Approval process and authorization rules</param>
/// <param name="NotificationRules">Notification and alerting rules</param>
public sealed record TenantBusinessRulesDto(Dictionary<string, object?> WorkflowRules, Dictionary<string, object?> ValidationRules, Dictionary<string, object?> ApprovalRules, Dictionary<string, object?> NotificationRules);

/// <summary>
///     User interface settings data transfer object
/// </summary>
/// <param name="Theme">UI theme and color scheme</param>
/// <param name="Layout">Layout preferences and customizations</param>
/// <param name="Branding">Branding and logo settings</param>
/// <param name="CustomCss">Custom CSS overrides</param>
/// <param name="ComponentSettings">Component-specific settings</param>
public sealed record TenantUiSettingsDto(string Theme, Dictionary<string, object?> Layout, TenantBrandingDto Branding, string? CustomCss, Dictionary<string, object?> ComponentSettings);

/// <summary>
///     Branding settings data transfer object
/// </summary>
/// <param name="LogoUrl">URL to the organization logo</param>
/// <param name="FaviconUrl">URL to the favicon</param>
/// <param name="PrimaryColor">Primary brand color</param>
/// <param name="SecondaryColor">Secondary brand color</param>
/// <param name="CompanyName">Company name for branding</param>
public sealed record TenantBrandingDto(string? LogoUrl, string? FaviconUrl, string? PrimaryColor, string? SecondaryColor, string? CompanyName);

/// <summary>
///     Security settings data transfer object
/// </summary>
/// <param name="PasswordPolicy">Password requirements and policies</param>
/// <param name="SessionTimeout">Session timeout settings</param>
/// <param name="TwoFactorRequired">Whether two-factor authentication is required</param>
/// <param name="IpWhitelist">IP address whitelist for access control</param>
/// <param name="ApiRateLimits">API rate limiting settings</param>
public sealed record TenantSecuritySettingsDto(Dictionary<string, object?> PasswordPolicy, int SessionTimeout, bool TwoFactorRequired, List<string> IpWhitelist, Dictionary<string, int> ApiRateLimits);

/// <summary>
///     Integration settings data transfer object
/// </summary>
/// <param name="ExternalServices">External service configurations</param>
/// <param name="WebhookSettings">Webhook endpoints and configurations</param>
/// <param name="ApiKeys">API key management and settings</param>
/// <param name="SsoConfiguration">Single sign-on configuration</param>
public sealed record TenantIntegrationSettingsDto(Dictionary<string, object?> ExternalServices, Dictionary<string, object?> WebhookSettings, Dictionary<string, string> ApiKeys, Dictionary<string, object?> SsoConfiguration);

/// <summary>
///     System limits data transfer object
/// </summary>
/// <param name="MaxUsers">Maximum number of users allowed</param>
/// <param name="MaxStorage">Maximum storage limit in bytes</param>
/// <param name="MaxApiCalls">Maximum API calls per period</param>
/// <param name="MaxProjects">Maximum number of projects</param>
/// <param name="CustomLimits">Additional custom resource limits</param>
public sealed record TenantSystemLimitsDto(int MaxUsers, long MaxStorage, int MaxApiCalls, int MaxProjects, Dictionary<string, int> CustomLimits);

/// <summary>
///     Request model for updating tenant settings
/// </summary>
/// <param name="SystemConfiguration">System configuration to update</param>
/// <param name="FeatureFlags">Feature flags to update</param>
/// <param name="BusinessRules">Business rules to update</param>
/// <param name="UserInterfaceSettings">UI settings to update</param>
/// <param name="SecuritySettings">Security settings to update</param>
/// <param name="IntegrationSettings">Integration settings to update</param>
/// <param name="SystemLimits">System limits to update</param>
public sealed record UpdateTenantSettingsRequest(
    UpdateTenantSystemConfigurationRequest? SystemConfiguration,
    Dictionary<string, bool>? FeatureFlags,
    UpdateTenantBusinessRulesRequest? BusinessRules,
    UpdateTenantUiSettingsRequest? UserInterfaceSettings,
    UpdateTenantSecuritySettingsRequest? SecuritySettings,
    UpdateTenantIntegrationSettingsRequest? IntegrationSettings,
    UpdateTenantSystemLimitsRequest? SystemLimits
);

/// <summary>
///     Request model for replacing tenant settings
/// </summary>
/// <param name="SystemConfiguration">Complete system configuration</param>
/// <param name="FeatureFlags">Complete feature flags</param>
/// <param name="BusinessRules">Complete business rules</param>
/// <param name="UserInterfaceSettings">Complete UI settings</param>
/// <param name="SecuritySettings">Complete security settings</param>
/// <param name="IntegrationSettings">Complete integration settings</param>
/// <param name="SystemLimits">Complete system limits</param>
public sealed record ReplaceTenantSettingsRequest(
    UpdateTenantSystemConfigurationRequest SystemConfiguration,
    Dictionary<string, bool> FeatureFlags,
    UpdateTenantBusinessRulesRequest BusinessRules,
    UpdateTenantUiSettingsRequest UserInterfaceSettings,
    UpdateTenantSecuritySettingsRequest SecuritySettings,
    UpdateTenantIntegrationSettingsRequest IntegrationSettings,
    UpdateTenantSystemLimitsRequest SystemLimits
);

/// <summary>
///     Request model for updating system configuration
/// </summary>
/// <param name="TimeZone">Time zone to update</param>
/// <param name="Locale">Locale to update</param>
/// <param name="DateFormat">Date format to update</param>
/// <param name="NumberFormat">Number format to update</param>
/// <param name="CurrencySettings">Currency settings to update</param>
/// <param name="CustomConfiguration">Custom configuration to update</param>
public sealed record UpdateTenantSystemConfigurationRequest(
    string? TimeZone,
    string? Locale,
    string? DateFormat,
    string? NumberFormat,
    UpdateTenantCurrencySettingsRequest? CurrencySettings,
    Dictionary<string, object?>? CustomConfiguration
);

/// <summary>
///     Request model for updating currency settings
/// </summary>
/// <param name="DefaultCurrency">Default currency to update</param>
/// <param name="DisplayFormat">Display format to update</param>
/// <param name="DecimalPlaces">Decimal places to update</param>
public sealed record UpdateTenantCurrencySettingsRequest(string? DefaultCurrency, string? DisplayFormat, int? DecimalPlaces);

/// <summary>
///     Request model for updating business rules
/// </summary>
/// <param name="WorkflowRules">Workflow rules to update</param>
/// <param name="ValidationRules">Validation rules to update</param>
/// <param name="ApprovalRules">Approval rules to update</param>
/// <param name="NotificationRules">Notification rules to update</param>
public sealed record UpdateTenantBusinessRulesRequest(
    Dictionary<string, object?>? WorkflowRules,
    Dictionary<string, object?>? ValidationRules,
    Dictionary<string, object?>? ApprovalRules,
    Dictionary<string, object?>? NotificationRules
);

/// <summary>
///     Request model for updating UI settings
/// </summary>
/// <param name="Theme">Theme to update</param>
/// <param name="Layout">Layout to update</param>
/// <param name="Branding">Branding to update</param>
/// <param name="CustomCss">Custom CSS to update</param>
/// <param name="ComponentSettings">Component settings to update</param>
public sealed record UpdateTenantUiSettingsRequest(string? Theme, Dictionary<string, object?>? Layout, UpdateTenantBrandingRequest? Branding, string? CustomCss, Dictionary<string, object?>? ComponentSettings);

/// <summary>
///     Request model for updating branding
/// </summary>
/// <param name="LogoUrl">Logo URL to update</param>
/// <param name="FaviconUrl">Favicon URL to update</param>
/// <param name="PrimaryColor">Primary color to update</param>
/// <param name="SecondaryColor">Secondary color to update</param>
/// <param name="CompanyName">Company name to update</param>
public sealed record UpdateTenantBrandingRequest(string? LogoUrl, string? FaviconUrl, string? PrimaryColor, string? SecondaryColor, string? CompanyName);

/// <summary>
///     Request model for updating security settings
/// </summary>
/// <param name="PasswordPolicy">Password policy to update</param>
/// <param name="SessionTimeout">Session timeout to update</param>
/// <param name="TwoFactorRequired">Two-factor requirement to update</param>
/// <param name="IpWhitelist">IP whitelist to update</param>
/// <param name="ApiRateLimits">API rate limits to update</param>
public sealed record UpdateTenantSecuritySettingsRequest(Dictionary<string, object?>? PasswordPolicy, int? SessionTimeout, bool? TwoFactorRequired, List<string>? IpWhitelist, Dictionary<string, int>? ApiRateLimits);

/// <summary>
///     Request model for updating integration settings
/// </summary>
/// <param name="ExternalServices">External services to update</param>
/// <param name="WebhookSettings">Webhook settings to update</param>
/// <param name="ApiKeys">API keys to update</param>
/// <param name="SsoConfiguration">SSO configuration to update</param>
public sealed record UpdateTenantIntegrationSettingsRequest(
    Dictionary<string, object?>? ExternalServices,
    Dictionary<string, object?>? WebhookSettings,
    Dictionary<string, string>? ApiKeys,
    Dictionary<string, object?>? SsoConfiguration
);

/// <summary>
///     Request model for updating system limits
/// </summary>
/// <param name="MaxUsers">Max users to update</param>
/// <param name="MaxStorage">Max storage to update</param>
/// <param name="MaxApiCalls">Max API calls to update</param>
/// <param name="MaxProjects">Max projects to update</param>
/// <param name="CustomLimits">Custom limits to update</param>
public sealed record UpdateTenantSystemLimitsRequest(int? MaxUsers, long? MaxStorage, int? MaxApiCalls, int? MaxProjects, Dictionary<string, int>? CustomLimits);

/// <summary>
///     Request model for updating feature flags
/// </summary>
/// <param name="FeatureFlags">Feature flags to update</param>
public sealed record UpdateTenantFeatureFlagsRequest(Dictionary<string, bool> FeatureFlags);
