# OpenAPI configuration

The API reads OpenAPI settings from the `OpenApi` configuration section. The same section configures the Swashbuckle document, security definitions, servers, schema documentation, Swagger UI, and localized documents. Configuration can come from `appsettings.json`, an environment-specific settings file, environment variables, or another ASP.NET Core configuration provider.

## Configuration example

```json
{
  "OpenApi": {
    "EnableOpenApi": true,
    "Title": "GameGuild API",
    "Description": "Public HTTP API for GameGuild.",
    "Version": "v1",
    "MetadataVersion": "2026.10",
    "ContactName": "GameGuild API Support",
    "ContactEmail": "api-support@example.com",
    "ContactUrl": "https://example.com/support",
    "TermsOfServiceUrl": "https://example.com/terms",
    "LicenseName": "Proprietary",
    "LicenseUrl": "https://example.com/license",
    "Servers": [
      {
        "Url": "https://api.example.com",
        "Description": "Production API"
      },
      {
        "Url": "https://{environment}.api.example.com",
        "Description": "Non-production API",
        "Variables": {
          "environment": {
            "Default": "staging",
            "Description": "Deployment environment",
            "Enum": [ "staging", "development" ]
          }
        }
      }
    ],
    "EnableDefaultBearer": true,
    "SecuritySchemes": {
      "PartnerKey": {
        "Kind": "ApiKeyHeader",
        "Description": "Key for approved service integrations.",
        "AuthenticationScheme": "PartnerKey",
        "HeaderName": "X-API-Key"
      },
      "CompanyOAuth": {
        "Kind": "OAuth2AuthorizationCode",
        "Description": "Company single sign-on.",
        "AuthenticationScheme": "CompanyOAuth",
        "AuthorizationUrl": "https://identity.example.com/authorize",
        "TokenUrl": "https://identity.example.com/token",
        "Scopes": {
          "openid": "Sign in with the company identity provider",
          "profile": "Read the user's basic profile"
        }
      }
    },
    "Schemas": {
      "Identity_Users_UserDto": {
        "Description": "Public account profile.",
        "ExampleJson": "{\"id\":\"00000000-0000-0000-0000-000000000001\",\"email\":\"developer@example.com\",\"name\":\"Ada Example\",\"createdAt\":\"2026-10-01T12:00:00Z\"}",
        "Properties": {
          "email": {
            "Description": "Verified email address for the account.",
            "ExampleJson": "\"developer@example.com\""
          }
        }
      }
    },
    "Extensions": {
      "x-api-audience": "\"developer\""
    },
    "Ui": {
      "RoutePrefix": "documentation",
      "DocumentTitle": "GameGuild API reference",
      "EnableDeepLinking": true,
      "EnableFilter": true,
      "DisplayRequestDuration": true,
      "PersistAuthorization": false
    },
    "Locales": {
      "pt-BR": {
        "Title": "API GameGuild",
        "Description": "Referência da API GameGuild.",
        "Tags": {
          "auth": "Autenticação e sessões"
        },
        "Operations": {
          "GET /v1/health": {
            "Summary": "Consultar a disponibilidade da API",
            "Description": "Retorna o estado atual do serviço."
          }
        },
        "Schemas": {
          "Identity_Users_UserDto": {
            "Description": "Perfil público da conta.",
            "Properties": {
              "email": "Endereço de e-mail verificado da conta."
            }
          }
        }
      }
    }
  }
}
```

`Version` is the fallback Swagger document key when API version discovery is unavailable. When API versioning is registered, discovered versions determine the document keys; `MetadataVersion` controls the release version shown in document metadata. Configure environment-specific server URLs in the matching `appsettings.{Environment}.json` file or configuration provider. Server URLs must be non-empty; absolute HTTP or HTTPS URLs are recommended for deployed environments. Contact, terms, license, and OAuth URLs must be absolute HTTP or HTTPS URLs.

The built-in Bearer scheme remains enabled by default for existing clients. Additional schemes are opt-in. Supported kinds are `ApiKeyHeader`, `HttpBearer`, `HttpBasic`, and `OAuth2AuthorizationCode`. To map a scheme to an operation, set its `AuthenticationScheme` to the ASP.NET authentication scheme used by that endpoint's `[Authorize(AuthenticationSchemes = "...")]` attribute. Unmatched schemes are documented but are not added to an operation's security requirements.

## Schema documentation

XML comments provide model and property descriptions when available. For schemas without an XML summary, the generated document supplies a readable description based on the schema ID and a synthetic example matching its OpenAPI type. These fallback examples are structural samples; they do not promise business-valid values for every domain rule. Configure `Schemas` to provide a domain-accurate `Description`, `ExampleJson`, or property documentation. Example strings must contain valid JSON and must use the serialized property names and enum values. The API includes curated, clearly synthetic examples for API-key, step-up, tenant-membership, and permission-review models.

## Code extension points

Use the product's `IApiProductComposition.ConfigureOpenApi` implementation to register product-specific Swashbuckle operation, schema, and document filters. The shared API setup also registers the built-in authorization-scheme, anonymous-endpoint, module-tag, server, localization, schema-documentation, and deterministic-ordering filters. Filters are code registrations because their types and dependencies cannot be safely represented as configuration values.

## Migration from inline Swagger setup

1. Move title, description, contact, terms, and license values from `SwaggerDoc` setup into the matching `OpenApi` metadata keys.
2. Move server URLs into `OpenApi:Servers`; use an environment-specific settings provider for deployment-specific values.
3. Keep the compatible default Bearer scheme enabled, and move additional security definitions into `OpenApi:SecuritySchemes`. Add `AuthenticationScheme` and endpoint `[Authorize]` attributes where operation-level security requirements are needed.
4. Move custom document extensions into `OpenApi:Extensions`. Each extension value is a JSON string (for example, `"\"developer\""` for a JSON string value).
5. Move schema summaries and examples into `OpenApi:Schemas` only when XML comments are not the preferred source. Keep custom operation, schema, and document filters in the product composition hook.
6. Move Swagger UI route and behavior settings into `OpenApi:Ui`. The API currently exposes Swagger JSON and the interactive UI only in Development and Staging by default; the compatibility `/openapi/{documentName}.json` URL redirects to the versioned Swagger JSON route.
7. Add translations under `OpenApi:Locales:{culture}`. Localized documents are generated alongside each discovered API version and leave the base document unchanged.

Validate configuration at startup. Invalid URLs, duplicate security scheme names, malformed JSON examples, invalid extension names, and invalid locale names cause setup to fail before serving the document.
