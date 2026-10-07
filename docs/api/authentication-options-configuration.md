# Authentication options configuration

The API host binds `PresentationLayer:Authentication` to the typed
`AuthenticationOptions` contract. The `SetupAuthentication` overload that is called without an options object
binds the root `Authentication` section; the host uses the nested presentation-layer section.

```json
{
  "PresentationLayer": {
    "Authentication": {
      "EnableAuthentication": true,
      "EnableAuthorization": true,
      "JwtIssuer": "GameGuild",
      "JwtAudience": "GameGuild.Users",
      "JwtExpiration": "01:00:00",
      "RefreshTokenExpirationDays": 30,
      "EnableApiKeyAuthentication": false,
      "ApiKeyHeaderName": "X-API-Key",
      "AllowApiKeyInQueryString": false,
      "ApiKeyQueryStringParameterName": "api_key",
      "EnableBasicAuthentication": false,
      "EnableCookieAuthentication": false,
      "PasswordPolicy": {
        "MinPasswordLength": 12,
        "MaxPasswordLength": 128,
        "RequireUppercase": true,
        "RequireLowercase": true,
        "RequireDigit": true,
        "RequireSpecialChar": true
      },
      "ExternalProviders": {
        "Providers": {
          "google": {
            "Enabled": false,
            "ClientId": "",
            "ClientSecret": "",
            "Scopes": []
          },
          "microsoft": {
            "Enabled": false,
            "ClientId": "",
            "ClientSecret": "",
            "Tenant": "common",
            "Scopes": []
          },
          "github": {
            "Enabled": false,
            "ClientId": "",
            "ClientSecret": "",
            "Scopes": []
          },
          "discord": {
            "Enabled": false,
            "ClientId": "",
            "ClientSecret": "",
            "Scopes": []
          }
        }
      }
    },
    "SecurityHeaders": {
      "EnableXContentTypeOptions": true,
      "EnableXFrameOptions": true,
      "XFrameOptionsValue": "DENY",
      "EnableContentSecurityPolicy": true
    }
  },
  "AuthenticationSecurity": {
    "MaxFailedAttemptsPerHour": 5,
    "MaxFailedAttemptsPerDay": 20,
    "MaxAttemptsPerIpPerHour": 50,
    "AccountLockoutDurationMinutes": 30,
    "EnableIpThrottling": true,
    "EnableUserEnumerationProtection": true
  },
  "Mfa": {
    "Enabled": true,
    "RequireMfaByDefault": false,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15
  },
  "Session": {
    "IdleTimeoutMinutes": 30,
    "AbsoluteTimeoutMinutes": 1440,
    "TerminateSessionsOnPasswordChange": true
  }
}
```

Supply secrets through the deployment secret store or the .NET environment configuration provider, for example:

```text
PresentationLayer__Authentication__JwtSecretKey=<deployment-secret>
PresentationLayer__Authentication__ExternalProviders__Providers__google__ClientSecret=<google-client-secret>
```

Options are validated at startup. Authentication stays JWT bearer by default; API keys, Basic, and cookie schemes
are opt-in additions. Basic and API-key query credentials require HTTPS. API-key query-string support is disabled
by default because URLs are commonly logged. Cookie authentication emits an HTTP-only, secure cookie with a
validated SameSite mode.

To enable the optional schemes, add only the settings the deployment needs:

```json
{
  "PresentationLayer": {
    "Authentication": {
      "EnableApiKeyAuthentication": true,
      "EnableBasicAuthentication": true,
      "Basic": {
        "SchemeName": "LegacyBasic",
        "Realm": "GameGuild API"
      },
      "EnableCookieAuthentication": true,
      "Cookie": {
        "SchemeName": "GameGuildCookie",
        "Name": "__Host-GameGuild.Auth",
        "Expiration": "08:00:00",
        "SlidingExpiration": false,
        "SameSite": "Strict"
      }
    }
  }
}
```

Production HSTS is enabled by the API pipeline; HSTS is not a mutable header value in
`SecurityHeadersOptions`. CSP and X-Frame-Options are configured through
`PresentationLayer:SecurityHeaders` and validated before the middleware is added.

API-key credentials can also be read from a deployment-specific request location in host code:

```csharp
var authentication = new AuthenticationOptions
{
    EnableApiKeyAuthentication = true,
    ApiKeyCustomKeyResolver = request => request.Headers["X-Deployment-Key"].FirstOrDefault()
};

services.SetupAuthentication(configuration, authentication);
```

The resolver is programmatic and cannot be represented safely in JSON configuration. It participates in the same
single-source check as the configured header and optional query parameter; supplying credentials through more than
one enabled source is rejected. Hosts can register other ASP.NET Core authentication schemes with the
`configureAdditionalSchemes` callback on `SetupAuthentication`.

Authentication-related concerns remain in their owning typed sections: account lockout and IP throttling use
`AuthenticationSecurity`, second-factor policy uses `Mfa`, session limits use `Session`, and response headers use
`PresentationLayer:SecurityHeaders`. The current platform uses the `GameGuild.Identity.Authentication` services and
repositories for account, password, OAuth, API-key, Basic, cookie, and MFA flows; the historical request to add a
second ASP.NET Core Identity persistence stack is superseded by that platform architecture.
