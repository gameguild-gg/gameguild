import { cache } from "react";
import {
  GameGuildAuth,
  CredentialsProvider,
  GoogleProvider,
  DiscordProvider,
  processSession,
  encodeSession,
  SessionStore,
  resolveCookieOptions,
} from "@game-guild/client";
import { cookies } from "next/headers";
import { createSharedAuthCookieConfig } from "@/lib/auth/cross-domain-auth";

const result = GameGuildAuth({
  providers: [
    CredentialsProvider(),
    ...(process.env.GOOGLE_CLIENT_ID && process.env.GOOGLE_CLIENT_SECRET
      ? [
          GoogleProvider({
            clientId: process.env.GOOGLE_CLIENT_ID,
            clientSecret: process.env.GOOGLE_CLIENT_SECRET,
          }),
        ]
      : []),
    ...(process.env.DISCORD_CLIENT_ID && process.env.DISCORD_CLIENT_SECRET
      ? [
          DiscordProvider({
            clientId: process.env.DISCORD_CLIENT_ID,
            clientSecret: process.env.DISCORD_CLIENT_SECRET,
          }),
        ]
      : []),
  ],
  pages: {
    // Callback failures redirect here with ?error=; the page renders it inline.
    error: "/sign-in",
  },
  apiUrl:
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    "http://localhost:8080",
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

async function readCurrentSession() {
  const cookieStore = await cookies();
  const cookieOptions = resolveCookieOptions(
    authConfig.cookies,
    authConfig.cookies.secure,
  );
  const sessionStore = new SessionStore(cookieOptions);
  const encrypted = sessionStore.read((name) => cookieStore.get(name)?.value);

  if (!encrypted) {
    return { session: null, token: null };
  }

  const { session, token, updated } = await processSession(
    encrypted,
    authConfig,
  );

  if (updated && token) {
    try {
      const newEncrypted = await encodeSession(token, authConfig);
      sessionStore.write(newEncrypted, (name, value, options) =>
        cookieStore.set(name, value, options),
      );
    } catch {
      // Server Components can read a valid refreshed session even when the
      // response context cannot persist its rotated cookie.
    }
  }

  return { session, token };
}

// Server Components must read Next's request-bound cookie store directly.
// The shared auth helper uses a dynamic next/headers import that is unavailable
// in the standalone RSC runtime, while the proxy continues to use auth().
/**
 * Resolves the complete authentication state once for the active request.
 *
 * Keeping the session, token, and tenant together prevents parallel API
 * clients from observing different rotations of the same session cookie.
 */
export const getRequestAuthContext = cache(async () => {
  const { session, token } = await readCurrentSession();
  return {
    session,
    token: token?.accessToken ?? null,
    tenantId: session?.tenantId ?? null,
  };
});

export async function getSession() {
  return (await getRequestAuthContext()).session;
}

export async function getToken(): Promise<string | null> {
  return (await getRequestAuthContext()).token;
}
