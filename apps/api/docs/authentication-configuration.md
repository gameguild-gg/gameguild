# Authentication configuration

The API binds the `Mfa` and `Session` sections when the Authentication module is registered. Both sections are validated at startup; invalid values stop application startup with a configuration error. Values can come from `appsettings.json`, environment-specific files, environment variables, or another ASP.NET Core configuration provider.

## Example

```json
{
  "Mfa": {
    "Enabled": true,
    "RequireMfaByDefault": false,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 15,
    "BackupCodesCount": 10,
    "BackupCodeLength": 8,
    "TotpIssuer": "GameGuild",
    "TotpTimeStepSeconds": 30,
    "TotpClockSkew": 1,
    "SetupSessionDurationMinutes": 10
  },
  "Session": {
    "IdleTimeoutMinutes": 30,
    "AbsoluteTimeoutMinutes": 1440,
    "MaxConcurrentSessions": 5,
    "TrustedDeviceDurationDays": 30,
    "MaxTrustedDevices": 10,
    "TerminateSessionsOnPasswordChange": true,
    "TerminateSessionsOnMfaDisable": true,
    "EnableDeviceFingerprinting": true,
    "EnableLocationTracking": true
  }
}
```

## MFA settings

| Setting | Runtime behavior |
| --- | --- |
| `Enabled` | Enables or disables TOTP and backup-code setup and verification. When disabled, MFA status and policy queries report that MFA is not required. |
| `RequireMfaByDefault` | Makes the MFA policy service require MFA by default for users when MFA is enabled. Elevated roles also require MFA. Automatic enforcement in every sign-in flow remains a separate integration requirement. |
| `MaxFailedAttempts`, `LockoutDurationMinutes` | Set the failed-code threshold and lockout duration for TOTP and backup-code verification. |
| `BackupCodesCount`, `BackupCodeLength` | Set how many codes setup produces and the length of each generated backup code. Codes are returned once and stored as hashes. |
| `TotpIssuer`, `TotpTimeStepSeconds`, `TotpClockSkew` | Set the authenticator-app issuer, the `otpauth` period, and the accepted time-step window. |
| `SetupSessionDurationMinutes` | Expires an unfinished TOTP setup. A late verification clears its pending secret and backup codes. |

## Session settings

| Setting | Runtime behavior |
| --- | --- |
| `IdleTimeoutMinutes` | Rejects a session that has had no activity for this long. |
| `AbsoluteTimeoutMinutes` | Caps session expiration from creation, including when a session is refreshed. |
| `MaxConcurrentSessions` | Deactivates the least recently used sessions before creating a session above this limit. |
| `TrustedDeviceDurationDays`, `MaxTrustedDevices` | Set the trusted-device lifetime and maximum active device count. Re-trusting a device renews its expiry. |
| `TerminateSessionsOnPasswordChange` | Terminates the user's sessions after a password change or reset. |
| `TerminateSessionsOnMfaDisable` | Terminates the user's sessions when MFA is disabled. |
| `EnableDeviceFingerprinting` | Controls fingerprint generation and trusted-device operations. When disabled, new sessions store no fingerprint. |
| `EnableLocationTracking` | Controls whether the session stores the supplied IP address; when disabled it stores `unknown`. |

`RequireTrustedDeviceForSensitiveOps` is present in the options model but is not yet enforced by sensitive-operation authorization. Do not rely on it as a security control until that integration is implemented.

This configuration documents the MFA and session runtime wiring only. It does not claim that the broader authentication-options issue is complete: cookie and Basic flows, complete ASP.NET Identity integration, end-to-end MFA enforcement during sign-in, and the remaining security and migration criteria still require reconciliation.
