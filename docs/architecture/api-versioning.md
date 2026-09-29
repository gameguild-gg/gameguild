# API versioning configuration

GameGuild configures ASP.NET API versioning through the `ApiVersioning` options section. The default parser remains the native ASP.NET format so existing `1.0` routes and controller attributes keep their current behavior.

```json
{
  "ApiVersioning": {
    "DefaultVersion": "1.0",
    "VersionFormat": "Native",
    "AssumeDefaultVersionWhenUnspecified": true,
    "ReadingStrategy": "UrlSegment",
    "ReportApiVersions": true,
    "SunsetPolicies": {
      "1.0": {
        "EffectiveAt": "2027-01-01T00:00:00Z",
        "PolicyUrl": "https://gameguild.gg/developer/api-versioning"
      }
    }
  }
}
```

`ReadingStrategy` supports URL segments, query strings, headers, media types, selected combinations, and all configured readers. `QueryParameterName`, `HeaderName`, and `MediaTypeParameterName` control the corresponding reader names. Invalid strategies, versions, and sunset policy URLs fail configuration validation instead of silently falling back.

## Version formats

`VersionFormat` accepts `Native` or `SemanticVersion`:

- `Native` uses the ASP.NET parser for major/minor versions, prerelease status, and date-based group versions such as `2026-09-29`.
- `SemanticVersion` additionally accepts numeric `major.minor.patch` values, prerelease identifiers such as `2.3.4-rc.1`, and build metadata such as `2.3.4+build.7`. Build metadata is preserved when formatting but does not affect precedence. Numeric components must not have leading zeroes, numeric prerelease identifiers must not have leading zeroes, and components larger than `Int32` are rejected.
- Date-based versions continue to use the native parser in either mode. Existing major/minor declarations remain compatible with semantic patch-zero requests.

Declare semantic versions with the attributes from `GameGuild.Configuration.PresentationLayer.ApiVersioning` so route metadata uses the same parser:

```csharp
[SemanticApiVersion("2.3.4")]
[Route("api/v{version:apiVersion}/widgets")]
public sealed class WidgetsController : ControllerBase { }

[SemanticApiVersion("2.4.0-beta.1")]
[Route("api/v{version:apiVersion}/preview-widgets")]
public sealed class PreviewWidgetsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetPreview() => Ok();
}
```

The custom version model preserves patch identity and applies semantic prerelease ordering. Use the matching semantic attributes whenever a controller or action declares a three-part version. Use `MapToSemanticApiVersion` when an action overrides the semantic version declared on its controller. Existing `ApiVersion` attributes remain valid for major/minor declarations.

## Deprecation and discovery

Set `ReportApiVersions` to advertise supported and deprecated versions in responses. Each `SunsetPolicies` entry is keyed by a version string understood by the selected parser; `EffectiveAt` configures the sunset date, and `PolicyUrl` adds the policy link. The versioned API Explorer continues to produce OpenAPI groups using `GroupNameFormat` and can substitute versions into URL templates.

## Remaining acceptance work for issue #144

The configuration, strict validation, native and semantic parsing, prerelease/date support, version reporting, sunset policy wiring, OpenAPI grouping, and URL-segment routing now have focused tests. The issue remains open until compatibility-matrix behavior, version-adoption logging and metrics, performance measurements, and a migration guide have been implemented and verified.
