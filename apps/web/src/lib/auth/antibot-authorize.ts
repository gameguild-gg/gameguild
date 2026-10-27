import {
  CredentialsSignInError,
  type CredentialsProviderConfig,
} from "@game-guild/client";

import { verifyTurnstileToken } from "@/lib/antibot/turnstile";

type CredentialsAuthorize = CredentialsProviderConfig["authorize"];

/**
 * Gate a credentials `authorize` behind Cloudflare Turnstile verification.
 *
 * Wire this around the innermost password authorize ONLY (see auth.ts):
 * the wrapper is applied at the password layer, so token-less flows that
 * bypass it (magic-link, MFA, web3) stay untouched — the exemption comes from
 * placement, plus an explicit `mfaToken` guard at the wrapped layer. The
 * default credentials provider also handles MFA completion (`mfaToken` +
 * `method`/`code`), which has no Turnstile widget by design (post-auth); the
 * `mfaToken` is a short-lived server-issued first-factor bearer, so possession
 * is already proven — same trust class as magic-link — and the call delegates
 * without verification.
 *
 * - `TURNSTILE_SECRET_KEY` unset/empty → delegate immediately, no verification
 *   call (dev/e2e fail-open; verifyTurnstileToken would fail open too, but this
 *   skips even the lookup).
 * - Otherwise the `credentials.turnstileToken` string is verified; failure
 *   throws `CredentialsSignInError` so the sign-in form shows a generic error.
 * - On success the ORIGINAL credentials object is delegated unchanged. The
 *   `turnstileToken` field must never reach a fetch body: the downstream
 *   authorize implementations build request bodies from fixed fields only —
 *   the default provider (packages/infrastructure/client/src/runtime/auth/
 *   providers/credentials.ts: `email/password/tenantId/rememberMe` or the MFA
 *   triple), magic-link (`token`/`tenantId`) and web3 — and unknown fields are
 *   ignored. Keep that true when touching those files.
 */
export function withAntibotVerification(authorize: CredentialsAuthorize): CredentialsAuthorize {
  return async (credentials, request) => {
    if (!process.env.TURNSTILE_SECRET_KEY) {
      return authorize(credentials, request);
    }

    // MFA completion (mfaToken + code) — first-factor possession already
    // proven by the server-issued short-lived bearer; no widget on the 2FA page.
    if (typeof credentials.mfaToken === "string" && credentials.mfaToken.length > 0) {
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
