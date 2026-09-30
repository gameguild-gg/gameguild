# Problem Details Configuration

Configure API errors through the `PresentationLayer:ProblemDetails` section. The API emits RFC 7807-compatible responses with the `application/problem+json` media type on Problem Details paths.

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

Exception mapping keys are fully qualified CLR type names. The mapper checks the thrown exception and its inner-exception chain, using the first matching type. Each mapping must provide an error status code, an absolute problem type URI, and a title. Mapping details are static public text; exception messages are not exposed by default.

## Detail levels

- `Minimal` omits the `detail` field.
- `Standard` returns configured public details and safe defaults.
- `Detailed` permits exception text only when `IncludeExceptionDetails` is also enabled.

Keep the base configuration at `Standard`. If developers need exception text locally, set both `DetailLevel` to `Detailed` and `IncludeExceptionDetails` to `true` in `appsettings.Development.json`; do not put that override in production configuration. Invalid combinations fail options validation during service registration.

## Localization and correlation

Localized messages are keyed by culture and message key. The current UI culture is checked first, followed by its parent culture. The default message key is `default`; database schema readiness errors use `database-schema-not-ready`; exception mappings can select another key with `LocalizedMessageKey`. Configure ASP.NET request localization before endpoint execution so the `Accept-Language` header selects the request culture.

When correlation IDs are enabled, the configured request header is returned in both the response header and the Problem Details extensions. A single printable value up to 128 characters is accepted. Missing, repeated, comma-joined, control-character, or whitespace-padded values fall back to ASP.NET's trace identifier.

`CustomExtensions` adds static string values to each Problem Details response. Extension names must not replace standard RFC fields, validation errors, exception text, or configured trace/correlation IDs.

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
