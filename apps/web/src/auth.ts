import { cache } from "react";
import {
  GameGuildAuth,
  CredentialsProvider,
  processSession,
  encodeSession,
  SessionStore,
  resolveCookieOptions,
} from "@game-guild/client";
import { cookies } from "next/headers";
import { createSharedAuthCookieConfig } from "@/lib/auth/cross-domain-auth";

const result = GameGuildAuth({
  providers: [CredentialsProvider()],
  apiUrl:
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    "http://localhost:5295",
  secret:
    process.env.AUTH_SECRET ||
    (process.env.NEXT_PHASE === "phase-production-build"
      ? "build-time-placeholder-not-used-at-runtime"
      : process.env.NODE_ENV === "development"
        ? "game-guild-web-local-development-secret"
        : undefined),
  debug: process.env.NODE_ENV === "development",
  cookies: createSharedAuthCookieConfig({
    authCookieDomain: process.env.AUTH_COOKIE_DOMAIN,
    authCookieSecure: process.env.AUTH_COOKIE_SECURE,
    nodeEnv: process.env.NODE_ENV,
  }),
});

export const { handlers, auth, signIn, signOut, signUp, update } = result;
export const authConfig = result.config;

/**
 * Get the raw access token from the session cookie.
 *
 * Wrapped in React.cache to deduplicate calls within the same request.
 * This is critical because createServerClient calls getAccessToken() twice
 * per request (requiresAuth guard + auth interceptor), and token refresh
 * with rotation would revoke the refresh token on the first call, causing
 * the second call to fail.
 */
export const getToken = cache(async (): Promise<string | null> => {
  const cookieStore = await cookies();
  const cookieOptions = resolveCookieOptions(
    authConfig.cookies,
    authConfig.cookies.secure,
  );
  const sessionStore = new SessionStore(cookieOptions);

  const encrypted = sessionStore.read((name) => cookieStore.get(name)?.value);
  if (!encrypted) {
    return null;
  }

  const { token, updated } = await processSession(encrypted, authConfig);

  // Persist refreshed token back to cookie so subsequent requests
  // use the new refresh token (backend does rotation).
  if (updated && token) {
    try {
      const newEncrypted = await encodeSession(token, authConfig);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      sessionStore.write(
        newEncrypted,
        (name: string, value: string, opts: any) => {
          cookieStore.set(name, value, opts);
        },
      );
    } catch {
      // Cookie may be read-only in some contexts (e.g. middleware)
    }
  }

  return token?.accessToken ?? null;
});
