/**
 * Auth Configuration Resolver
 *
 * Resolves the user-provided GameGuildAuthConfig into a fully resolved
 * config with all defaults applied.
 */

import type { GameGuildAuthConfig, ResolvedAuthConfig, AuthCallbacks } from '../../runtime/auth/types.js';
import { MissingSecretError, ConfigError } from '../../runtime/auth/errors.js';

/** Default max age: 30 days */
const DEFAULT_MAX_AGE = 30 * 24 * 60 * 60;

/**
 * Default callbacks — pass-through implementations
 */
const defaultCallbacks: Required<AuthCallbacks> = {
  jwt: async ({ token }) => token,
  session: async ({ session }) => session,
  signIn: async () => true,
  redirect: async ({ url, baseUrl }) => {
    // Allow relative URLs
    if (url.startsWith('/')) return `${baseUrl}${url}`;
    // Allow same-origin URLs
    try {
      const urlObj = new URL(url);
      const baseObj = new URL(baseUrl);
      if (urlObj.origin === baseObj.origin) return url;
    } catch {
      // Invalid URL
    }
    return baseUrl;
  },
  authorized: async ({ auth }) => !!auth,
};

/**
 * Resolve a user config into a fully-qualified config with defaults.
 */
export function resolveConfig(config: GameGuildAuthConfig): ResolvedAuthConfig {
  // Resolve secret
  const secret =
    config.secret ||
    (typeof process !== 'undefined' ? process.env?.AUTH_SECRET : undefined) ||
    (typeof process !== 'undefined' ? process.env?.NEXTAUTH_SECRET : undefined) ||
    '';

  if (!secret) {
    throw new MissingSecretError();
  }

  // Resolve API URL
  const apiUrl =
    config.apiUrl ||
    (typeof process !== 'undefined' ? process.env?.API_URL : undefined) ||
    (typeof process !== 'undefined' ? process.env?.NEXT_PUBLIC_API_URL : undefined) ||
    '';

  if (!apiUrl) {
    throw new ConfigError('Missing API URL. Set apiUrl in GameGuildAuth config or API_URL environment variable.');
  }

  // Determine if secure (HTTPS)
  const isSecure = config.cookies?.secure ?? (typeof process !== 'undefined' ? process.env?.NEXTAUTH_URL?.startsWith('https') : false) ?? false;

  // Merge callbacks
  const callbacks: Required<AuthCallbacks> = {
    jwt: config.callbacks?.jwt ?? defaultCallbacks.jwt,
    session: config.callbacks?.session ?? defaultCallbacks.session,
    signIn: config.callbacks?.signIn ?? defaultCallbacks.signIn,
    redirect: config.callbacks?.redirect ?? defaultCallbacks.redirect,
    authorized: config.callbacks?.authorized ?? defaultCallbacks.authorized,
  };

  return {
    providers: config.providers,
    callbacks,
    secret,
    apiUrl,
    pages: config.pages ?? {},
    cookies: {
      name: config.cookies?.name ?? '__me',
      secure: isSecure,
      sameSite: config.cookies?.sameSite ?? 'lax',
      path: config.cookies?.path ?? '/',
      domain: config.cookies?.domain,
      maxAge: config.cookies?.maxAge ?? DEFAULT_MAX_AGE,
      httpOnly: config.cookies?.httpOnly ?? true,
    },
    maxAge: config.maxAge ?? DEFAULT_MAX_AGE,
    updateAge: config.updateAge ?? 0,
    basePath: config.basePath ?? '/api/auth',
    debug: config.debug ?? false,
    trustHost: config.trustHost ?? false,
    tenantHeader: config.tenantHeader ?? 'X-Tenant-Id',
  };
}
