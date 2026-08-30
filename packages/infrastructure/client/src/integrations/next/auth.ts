/**
 * GameGuildAuth — Main Auth Factory
 *
 * The entry point for configuring authentication in a Next.js app.
 * Inspired by NextAuth(config) but tailored for the GameGuild .NET backend.
 *
 * @example
 * ```typescript
 * // src/auth.ts
 * import { GameGuildAuth, CredentialsProvider } from '@game-guild/client/next';
 *
 * export const { handlers, auth, signIn, signOut, signUp, update } = GameGuildAuth({
 *   providers: [
 *     CredentialsProvider(),
 *   ],
 *   callbacks: {
 *     // Customize the JWT — e.g., add tenant switching
 *     async jwt({ token, trigger, session }) {
 *       if (trigger === 'update' && session?.tenantId) {
 *         token.tenantId = session.tenantId;
 *       }
 *       return token;
 *     },
 *     // Customize what's exposed to the client
 *     async session({ session, token }) {
 *       session.tenantId = token.tenantId;
 *       return session;
 *     },
 *   },
 * });
 * ```
 *
 * Then in your route handler:
 * ```typescript
 * // src/app/api/auth/[...auth]/route.ts
 * import { handlers } from '@/auth';
 * export const { GET, POST } = handlers;
 * ```
 *
 * And in proxy:
 * ```typescript
 * // src/proxy.ts
 * import { auth } from '@/auth';
 * export default auth((req) => {
 *   if (!req.auth && req.nextUrl.pathname !== '/sign-in') {
 *     return Response.redirect(new URL('/sign-in', req.nextUrl.origin));
 *   }
 * });
 * ```
 */

import type { GameGuildAuthConfig, AuthInstance } from '../../runtime/auth/types.js';
import { resolveConfig } from './config.js';
import { createHandlers } from './handlers.js';
import { createAuthFunction, createSignInAction, createSignUpAction, createSignOutAction, createUpdateAction } from './actions.js';

/**
 * Initialize the GameGuild authentication system.
 *
 * Returns an object with all auth utilities:
 * - `handlers` — Route handlers for /api/auth/[...auth]
 * - `auth` — Get session / use as proxy
 * - `signIn` — Server Action for sign-in
 * - `signUp` — Server Action for sign-up
 * - `signOut` — Server Action for sign-out
 * - `update` — Server Action to update the session
 *
 * @param userConfig - Authentication configuration
 * @returns Auth instance with all utilities
 */
export function GameGuildAuth(userConfig: GameGuildAuthConfig): AuthInstance {
  const config = resolveConfig(userConfig);

  // Create route handlers
  const { GET, POST } = createHandlers(config);

  // Create auth function (session reader / proxy wrapper)
  const authFn = createAuthFunction(config);

  // Create server actions
  const signIn = createSignInAction(config);
  const signUp = createSignUpAction(config);
  const signOut = createSignOutAction(config);
  const update = createUpdateAction(config);

  return {
    handlers: { GET, POST },
    auth: authFn as AuthInstance['auth'],
    signIn,
    signUp,
    signOut,
    update,
    config,
  };
}
