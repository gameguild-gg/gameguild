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
} from './types';

// Token refresh manager
export { TokenRefreshManager, type TokenRefreshConfig } from './refresh';

// JWT encrypt/decrypt
export { encodeJWT, decodeJWT } from './jwt';

// Cookie management
export {
  SessionStore,
  CsrfStore,
  CallbackStore,
  resolveCookieOptions,
  getCookieName,
  type ResolvedCookieOptions,
  type CookieSerializeOptions,
} from './cookies';

// CSRF protection
export { createCSRFToken, validateCSRFToken } from './csrf';

// Session management
export { createJWTPayload, toSession, shouldRefreshToken, refreshAccessToken, processSession, encodeSession } from './session';

// Error types
export {
  AuthError,
  CredentialsSignInError,
  AccountLockedError,
  AuthServiceUnavailableError,
  MfaRequiredError,
  parseMfaEnrollmentOffer,
  type MfaEnrollmentOffer,
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
} from './errors';

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
} from './providers/index';
