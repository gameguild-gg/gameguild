import {
  CredentialsSignInError,
  type CredentialsProviderConfig,
} from "@game-guild/client";

import { verifyTurnstileToken } from "@/lib/antibot/turnstile";

type CredentialsAuthorize = CredentialsProviderConfig["authorize"];

/**
 * Gate a standalone credentials boundary that owns Turnstile verification.
 *
 * Password sign-in and sign-up in auth.ts do not use this wrapper: their API
 * consumes the single-use proof, forwarded by the SDK. Pre-verifying it here
 * would make the API reject a legitimate token as already used.
 *
 * For a boundary that owns verification, an unset secret delegates immediately.
 * Passwordless MFA completion also delegates; mixed password/MFA payloads still
 * require verification. Success passes the original credentials object unchanged.
 */
export function withAntibotVerification(authorize: CredentialsAuthorize): CredentialsAuthorize {
  return async (credentials, request) => {
    if (!process.env.TURNSTILE_SECRET_KEY) {
      return authorize(credentials, request);
    }

    // MFA completion (mfaToken + code, never a password) — first-factor
    // possession already proven by the server-issued short-lived bearer; no
    // widget on the 2FA page. Exempt ONLY when no password is present: a
    // password attempt with an appended bogus mfaToken must NOT skip Turnstile.
    const isPasswordlessMfaCompletion =
      typeof credentials.mfaToken === "string" &&
      credentials.mfaToken.length > 0 &&
      (typeof credentials.password !== "string" || credentials.password.length === 0);
    if (isPasswordlessMfaCompletion) {
      return authorize(credentials, request);
    }

    const token = credentials.turnstileToken;
    const verification = await verifyTurnstileToken(typeof token === "string" ? token : undefined);

    if (!verification.success) {
      throw new CredentialsSignInError("Bot verification failed");
    }

    return authorize(credentials, request);
  };
}
