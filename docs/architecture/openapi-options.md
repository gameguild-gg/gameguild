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
    },
    "Extensions": {
      "x-api-audience": "{\"roles\":[\"developer\",\"operator\"],\"public\":true}"
    },
    "Schemas": {
      "Identity_Users_UserDto": {
        "Description": "The public account profile returned by this API.",
        "ExampleJson": "{\"id\":\"00000000-0000-0000-0000-000000000001\",\"email\":\"ada@example.com\",\"name\":\"Ada\",\"createdAt\":\"2026-09-30T12:00:00Z\"}",
        "Properties": {
          "id": {
            "Description": "Stable account identifier.",
            "ExampleJson": "\"00000000-0000-0000-0000-000000000001\""
          }
        }
      }
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

`Extensions` maps OpenAPI `x-` names to JSON-encoded values. An extension value
can therefore be a string, number, boolean, array, object, or `null`. The
options validator rejects invalid names, malformed JSON, and values over 64 KiB
on startup. `Schemas` uses the generated schema ID, not a C# type name; the
schema IDs visible in `/swagger/v1/swagger.json` are the source of truth. A
configured description or example replaces that schema's generated value, while
unconfigured fields keep the existing output. `Properties` uses serialized
property names. Examples must match the model's wire representation; JSON
syntax is validated at startup, and the API owner must verify semantic validity.
The default document is unchanged when these dictionaries are empty.

This configuration adds structured extension data and opt-in schema descriptions
and examples for issue #147. Language-specific documents, broad model coverage,
and final generated-document verification remain required before that issue can
close.

## Document generation measurement

The `OpenApiDocumentGenerationBenchmarks` fixture generates the full `v1`
document with default options and with one structured extension and one schema
override. The 2026-09-30 local BenchmarkDotNet `ShortRun` used one launch, three
warmups, and three measured iterations on Windows 10, .NET 10.0.10, and an Intel
Xeon W-2235. The document has 1,261 paths and 1,579 schemas.

| Configuration | Mean | Error | Allocated |
| --- | ---: | ---: | ---: |
| Default | 708.3 ms | 376.1 ms | 223.97 MB |
| Configured | 690.9 ms | 1,444.4 ms | 223.97 MB |

The error intervals overlap substantially, so this short local run does not
establish a speed difference. It measures document generation only, without
HTTP startup, serialization, or downstream client generation. Reproduce it from
the repository root with:

```powershell
dotnet run --project apps/api/tests/GameGuild.API.Versioning.PerformanceTests/GameGuild.API.Versioning.PerformanceTests.csproj --configuration Release -- --filter '*OpenApiDocumentGenerationBenchmarks*' --job Short --buildTimeout 600
```
