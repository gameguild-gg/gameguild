import { createClient } from '../../client';
import { AuthModule } from '../../generated/modules/auth.gen';
import type { JWTPayload } from './types';

/** Revoke only the refresh credential owned by this decrypted session's actor. */
export async function revokeSessionRefreshToken(apiUrl: string, session: Pick<JWTPayload, 'accessToken' | 'refreshToken'>): Promise<void> {
  const { accessToken, refreshToken } = session;
  if (typeof accessToken !== 'string' || !accessToken.trim() || typeof refreshToken !== 'string' || !refreshToken.trim()) {
    return;
  }
  const auth = new AuthModule(
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
  await auth.postAuthTokensRevoke({ token: refreshToken });
}
