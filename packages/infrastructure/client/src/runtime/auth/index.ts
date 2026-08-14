/**
 * Auth Module
 *
 * Re-exports all auth-related types and utilities.
 * This is the framework-agnostic core — no React or Next.js dependencies.
 */

// Core types
export type {
  AuthConfig,
  AuthMode,
  TokenPair,
  TokenProvider,
  Session,
  SessionUser,
  JWTPayload,
  GameGuildAuthConfig,
  ResolvedAuthConfig,
  AuthCallbacks,
  AuthInstance,
  CookieConfig,
  PagesConfig,
  Provider,
  ProviderConfig,
  ProviderType,
  ProviderResult,
  CredentialsProviderConfig,
  OAuthProviderConfig,
  SessionStatus,
  UseSessionReturn,
  SessionProviderProps,
} from './types.js';

// Token refresh manager
export { TokenRefreshManager, type TokenRefreshConfig } from './refresh.js';

// JWT encrypt/decrypt
export { encodeJWT, decodeJWT } from './jwt.js';

// Cookie management
export {
  SessionStore,
  CsrfStore,
  CallbackStore,
  resolveCookieOptions,
  getCookieName,
  type ResolvedCookieOptions,
  type CookieSerializeOptions,
} from './cookies.js';

// CSRF protection
export { createCSRFToken, validateCSRFToken } from './csrf.js';

// Session management
export {
  createJWTPayload,
  toSession,
  shouldRefreshToken,
  refreshAccessToken,
  processSession,
  encodeSession,
} from './session.js';

// Error types
export {
  AuthError,
  CredentialsSignInError,
  AccountLockedError,
  MfaRequiredError,
  SignUpError,
  SessionExpiredError,
  InvalidSessionError,
  TokenRefreshError,
  ConfigError,
  MissingSecretError,
  ProviderNotFoundError,
  CSRFError,
  OAuthError,
  OAuthCallbackError,
  isAuthError,
  isReauthRequired,
  isCredentialsError,
} from './errors.js';

// Provider factories
export {
  CredentialsProvider,
  GoogleProvider,
  GitHubProvider,
  DiscordProvider,
  type CredentialsProviderOptions,
  type GoogleProviderOptions,
  type GitHubProviderOptions,
  type DiscordProviderOptions,
} from './providers/index.js';
