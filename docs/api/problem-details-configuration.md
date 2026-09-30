# Problem Details Configuration

Configure API errors through the `PresentationLayer:ProblemDetails` section. Error responses use RFC 7807 Problem Details with the `application/problem+json` media type across exception middleware, MVC error results, model validation, and otherwise-empty routing or minimal API error responses.

```json
{
  "PresentationLayer": {
    "ProblemDetails": {
      "DetailLevel": "Standard",
      "DefaultType": "about:blank",
      "DefaultTitle": "An error occurred.",
      "DefaultDetail": "An unexpected error occurred.",
      "IncludeInstance": true,
      "IncludeTraceId": true,
      "TraceIdExtensionName": "traceId",
      "IncludeCorrelationId": true,
      "CorrelationIdHeaderName": "X-Correlation-ID",
      "CorrelationIdExtensionName": "correlationId",
      "ExceptionMappings": {
        "System.ArgumentException": {
          "StatusCode": 400,
          "Type": "https://api.gameguild.gg/problems/invalid-request",
          "Title": "Invalid request",
          "Detail": "The submitted request is invalid.",
          "LocalizedMessageKey": "invalid-request"
        }
      },
      "LocalizedMessages": {
        "pt-BR": {
          "invalid-request": {
            "Title": "Solicitação inválida",
            "Detail": "Os dados enviados são inválidos."
          }
        }
      },
      "CustomExtensions": {
        "service": "GameGuild API"
      }
    }
  }
}
```

Exception mapping keys are fully qualified CLR type names. The mapper checks each thrown exception, its base types, and its inner-exception chain, using the first matching type. The defaults map `ArgumentException` (including subclasses) and `System.Text.Json.JsonException` to HTTP 400. Each mapping must provide an error status code, an absolute problem type URI, and a title. Mapping details are static public text; exception messages are not exposed by default.

## Detail levels

- `Minimal` omits the `detail` field.
- `Standard` returns configured public details and safe defaults.
- `Detailed` permits exception text only when `IncludeExceptionDetails` is also enabled.

Keep the base configuration at `Standard`. If developers need exception text locally, set both `DetailLevel` to `Detailed` and `IncludeExceptionDetails` to `true` in `appsettings.Development.json`. The runtime also checks the hosting environment, so exception details are never returned outside Development even if the option is accidentally enabled. Invalid combinations fail options validation during service registration.

## Localization and correlation

Localized messages are keyed by culture and message key. The current UI culture is checked first, followed by its parent culture. The default message key is `default`; built-in mappings use `invalid-argument` and `invalid-json`; domain errors use their stable `code` as the key; database schema readiness errors use `database-schema-not-ready`; custom exception mappings can select another key with `LocalizedMessageKey`. Configure ASP.NET request localization before endpoint execution so the `Accept-Language` header selects the request culture.

When correlation IDs are enabled, the configured request header is returned in both the response header and the Problem Details extensions. A single printable value up to 128 characters is accepted. Missing, repeated, comma-joined, control-character, or whitespace-padded values fall back to ASP.NET's trace identifier.

`CustomExtensions` adds static string values to each Problem Details response. Extension names must not replace standard RFC fields, error `code`, validation errors, exception text, the preserved `legacy` payload, or configured trace/correlation IDs. Domain errors expose their stable machine-readable code as a separate `code` extension while retaining their human-readable description in `detail`.

## Migration guide

- Existing `BadRequest()`, `NotFound()`, and other MVC 4xx/5xx results now include a Problem Details body. String messages move to `detail`; other legacy response objects are preserved under the `legacy` extension. Clients should read the HTTP status and the `type` field instead of assuming the response body is empty.
- MVC model-validation responses keep the `errors` dictionary and every field message; common trace, correlation, instance, and custom extension fields are added alongside it.
- Existing controller-provided `ProblemDetails` keeps its explicit title, detail, and type unless `DetailLevel` is `Minimal`; shared defaults and extensions are added consistently.
- Unhandled exceptions keep a generic public response by default. Map expected exception classes in configuration to stable problem types and safe public descriptions; do not depend on raw exception text.
- During rollout, update generated clients or error handlers that only recognize the previous ad-hoc response shapes, and add coverage for both non-empty Problem Details and empty status-only failures.

## Builder usage

The options can also be configured directly in code:

```csharp
builder.AddPresentationLayer(options =>
{
    options.ProblemDetails!.DetailLevel = ProblemDetailsDetailLevel.Standard;
    options.ProblemDetails.IncludeCorrelationId = true;
});
```

Use `ProblemDetailsOptionsBuilder.Build(configuration)` when a component needs to bind and validate a standalone `ProblemDetails` section outside the presentation-layer options tree.
