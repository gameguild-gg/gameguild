import { createClient } from '../../client';
import { AuthModule } from '../../generated/modules/auth.gen';
import type { JWTPayload } from './types';

function createLogoutAuthModule(apiUrl: string, accessToken: string): AuthModule {
  return new AuthModule(
    createClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => Promise.resolve(accessToken) },
      // Logout must not rotate the credential it is about to revoke or create a
      // replacement session. Server failure still allows the caller's local cleanup.
      autoRefresh: false,
      timeout: 5_000,
      devtools: { enabled: false },
    }),
  );
}

/** Revoke only the refresh credential owned by this decrypted session's actor. */
export async function revokeSessionRefreshToken(apiUrl: string, session: Pick<JWTPayload, 'accessToken' | 'refreshToken'>): Promise<void> {
  const { accessToken, refreshToken } = session;
  if (typeof accessToken !== 'string' || !accessToken.trim() || typeof refreshToken !== 'string' || !refreshToken.trim()) {
    return;
  }
  const auth = createLogoutAuthModule(apiUrl, accessToken);
  await auth.postAuthTokensRevoke({ token: refreshToken });
}

/**
 * Sign the session out server-side via the dedicated sign-out endpoint (issue #1196),
 * through the generated API client. The endpoint deny-lists the current access
 * token's jti (so a copied access token dies at once instead of living to its exp)
 * and, with logoutEverywhere, also revokes every refresh token and terminates every
 * session for the account. When the sign-out endpoint answers with an HTTP failure
 * (e.g. the access token already expired), falls back to revoking just the refresh
 * token; when the API is unreachable or stalled (timeout/network), the fallback is
 * skipped so the five-second logout bound is never doubled.
 */
export async function signOutServerSession(
  apiUrl: string,
  session: Pick<JWTPayload, 'accessToken' | 'refreshToken'>,
  logoutEverywhere = false,
): Promise<void> {
  const { accessToken } = session;
  if (typeof accessToken === 'string' && accessToken.trim()) {
    let failedAtHttpLevel = false;
    try {
      const auth = createLogoutAuthModule(apiUrl, accessToken);
      const result = await auth.postAuthSignOut({ logoutEverywhere });
      if (result.ok) {
        return;
      }
      // Transport-level failures carry status 0 with TIMEOUT/NETWORK_ERROR codes:
      // the API is unreachable or stalled, so a revocation attempt would only
      // double the logout bound without reaching the server either.
      failedAtHttpLevel = result.error.status !== 0 && result.error.code !== 'TIMEOUT' && result.error.code !== 'NETWORK_ERROR';
    } catch {
      // The endpoint answered (e.g. an unparseable empty body) — treat the
      // server-side sign-out as attempted and skip further server calls.
      return;
    }
    if (!failedAtHttpLevel) {
      return;
    }
    // HTTP-level failure: the refresh token may still be revocable — try that
    // before the caller's local cleanup.
  }

  await revokeSessionRefreshToken(apiUrl, session);
}
