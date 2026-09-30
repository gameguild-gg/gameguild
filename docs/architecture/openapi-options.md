# OpenAPI options

The API binds the `OpenApi` section to `OpenApiOptions` and validates it during
presentation-layer setup. The current default still advertises the existing
`Bearer` definition and `/documentation` Swagger UI route, so consumers of the
generated document keep their existing configuration.

```json
{
  "OpenApi": {
    "Title": "GameGuild API",
    "Description": "Developer API",
    "MetadataVersion": "2026.09",
    "Servers": [
      {
        "Url": "https://api.example.com/{version}",
        "Description": "Production",
        "Variables": {
          "version": { "Default": "v1", "Enum": ["v1", "v2"] }
        }
      }
    ],
    "SecuritySchemes": {
      "ApiKey": {
        "Kind": "ApiKeyHeader",
        "HeaderName": "X-API-Key",
        "AuthenticationScheme": "ApiKey",
        "Description": "API key issued by GameGuild"
      },
      "ExternalOAuth": {
        "Kind": "OAuth2AuthorizationCode",
        "AuthorizationUrl": "https://identity.example.com/authorize",
        "TokenUrl": "https://identity.example.com/token",
        "Scopes": { "api.read": "Read API data" },
        "AuthenticationScheme": "ExternalOAuth"
      }
    },
    "Ui": {
      "RoutePrefix": "developer/docs",
      "DocumentTitle": "GameGuild developer API",
      "EnableFilter": true,
      "EnableDeepLinking": true,
      "DisplayRequestDuration": true,
      "PersistAuthorization": false
    }
  }
}
```

`SecuritySchemes` may also contain `HttpBearer` or `HttpBasic` entries. Their
names are OpenAPI definition names; `AuthenticationScheme` must match a scheme
registered by ASP.NET Core when an action explicitly selects it with
`[Authorize(AuthenticationSchemes = "...")]`. The OpenAPI operation filter maps
those explicit selections to the matching definitions. It leaves the default
Bearer requirement on other actions, and the existing anonymous filter removes
requirements from `[AllowAnonymous]` actions. Defining an OAuth2 scheme here
documents an existing provider; it does not register an authentication handler.

`EnableDefaultBearer` can be set to `false` when migrating a document to other
schemes. Keep it enabled until generated clients and documented authorization
requirements have been reviewed. OAuth2 URLs, API key header names, scheme
names, and UI route prefixes are validated on startup.

Swagger JSON and UI are currently served in Development and Staging. The
`/openapi/{documentName}.json` compatibility route redirects to the corresponding
Swashbuckle document. Versioned documents retain their existing URL structure.

This configuration covers security definitions and UI options from issue #147.
Schema examples, language-specific document generation, configurable extension
data, and document-generation performance evidence still need separate
acceptance work before that issue can close.
