/**
 * @game-guild/client
 *
 * Type-safe API client for the GameGuild platform.
 *
 * @example
 * ```typescript
 * import { createClient } from '@game-guild/client';
 *
 * const client = createClient({
 *   baseUrl: 'https://api.gameguild.com',
 * });
 *
 * const result = await client.users.getProfile('user-123');
 * if (result.ok) {
 *   console.log(result.data);
 * }
 * ```
 */

// Client factories
export { createClient, type ClientConfig } from './client';
export { createServerClient, type ServerClientConfig } from './server';

// Result types
export type { Result, Ok, Err, ResultData, ResultError } from './runtime/result/types';
export { ok, err, isOk, isErr, unwrap, unwrapOr, match } from './runtime/result/helpers';

// Error types and guards
export type { ApiError, ApiErrorCode, AuthenticationError, AuthorizationError, NotFoundError, ValidationError } from './runtime/errors/types';

export {
  isApiError,
  isUnauthorized,
  isForbidden,
  isInsufficientPermissions,
  isFeatureNotAvailable,
  isNotFoundError,
  isValidationError,
  isNetworkError,
  isRetryableError,
  getRequiredPermissions,
  getRequiredFeature,
} from './runtime/errors/guards';

// Validation utilities
export { transformZodError, isZodError, safeParse } from './runtime/errors/validation';

// DevTools
export { DevTools, type DevToolsConfig } from './runtime/devtools/index';

// Deduplication
export { RequestDeduplicator, type DeduplicationConfig } from './runtime/deduplication/index';

// Product API exports
export * from './product-exports';

// Additional error guard
export { getRetryAfter } from './runtime/errors/guards';

// Auth types
export type {
  TokenProvider,
  TokenPair,
  AuthConfig,
  Session,
  SessionUser,
  JWTPayload,
  GameGuildAuthConfig,
  AuthCallbacks,
  ProviderType,
  ProviderConfig,
  ProviderResult,
  CredentialsProviderConfig,
  OAuthProviderConfig,
  Provider,
  CookieConfig,
  PagesConfig,
  AuthInstance,
  ResolvedAuthConfig,
  SessionStatus,
  UseSessionReturn,
  SessionProviderProps,
} from './runtime/auth/types';

// Auth utilities
export { encodeJWT, decodeJWT } from './runtime/auth/jwt';
export { SessionStore, CsrfStore, CallbackStore, resolveCookieOptions } from './runtime/auth/cookies';
export { createCSRFToken, validateCSRFToken } from './runtime/auth/csrf';
export { createJWTPayload, toSession, shouldRefreshToken, refreshAccessToken, processSession, encodeSession } from './runtime/auth/session';

// Auth errors
export {
  AuthServiceUnavailableError,
  AuthError,
  CredentialsSignInError,
  AccountLockedError,
  MfaRequiredError,
  parseMfaEnrollmentOffer,
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
  MfaVerificationError,
  PasswordResetError,
  EmailVerificationError,
  SessionTerminationError,
  parseErrorBody,
  extractErrorMessage,
  isAuthError,
  isReauthRequired,
  isCredentialsError,
} from './runtime/auth/errors';

// Authorization utilities
export { hasRole, hasAllRoles, hasAnyRole, hasPermission, hasAllPermissions, hasAnyPermission, can } from './runtime/auth/authorization';

// Extended auth operations (MFA, password reset, email verification, session management)
export {
  verifyMfa,
  setupTotpMfa,
  getMfaMethods,
  requestPasswordReset,
  confirmPasswordReset,
  changePassword,
  sendVerificationEmail,
  resendVerificationEmail,
  verifyEmail,
  listSessions,
  terminateSession,
  terminateOtherSessions,
  terminateAllSessions,
} from './runtime/auth/extended-operations';
export type {
  MfaVerifyInput,
  MfaSetupResult,
  PasswordResetRequestInput,
  PasswordResetConfirmInput,
  PasswordChangeInput,
  EmailVerificationInput,
  SessionInfo,
} from './runtime/auth/extended-operations';

// Auth providers
export { CredentialsProvider } from './runtime/auth/providers/credentials';
export { DiscordProvider } from './runtime/auth/providers/discord';
export { GitHubProvider } from './runtime/auth/providers/github';
export { GoogleProvider } from './runtime/auth/providers/google';

// Tenant types
export type { TenantProvider, TenantConfig } from './runtime/tenant/types';

// Transport types
export type { RequestConfig, ApiResponse, Interceptor } from './runtime/transport/types';

// ─── Next.js Integration ─────────────────────────────────────────
export { GameGuildAuth } from './integrations/next/auth';
export { parseCookieHeader, parseBackendAuthResponse } from './integrations/next/handlers';
export { createMfaEnrollmentCompleteAction, type MfaEnrollmentCompleteResult } from './integrations/next/actions';
export type { OAuthProviderWithMethods } from './integrations/next/oauth-helpers';
export {
  createNextClient,
  createNextAuthTokenProvider,
  createNextTenantProvider,
  createClientFromCookies,
  createRouteClient,
  type NextClientConfig,
} from './integrations/next/index';

// ─── Plugins ─────────────────────────────────────────────────────
export { createRetryPlugin, createRetryInterceptor, type RetryConfig } from './plugins/retry';
export { createAuthRetryPlugin, type AuthRetryConfig } from './plugins/auth-retry';
export { createLoggingInterceptor, type LoggingConfig, type LogLevel, type LoggerFn } from './plugins/logging';
export { createCacheInterceptor, MemoryCache, type CacheConfig, type CacheInterceptor } from './plugins/cache';
export { createMetricsInterceptor, type MetricsConfig, type RequestMetrics, type AggregatedMetrics, type MetricsInterceptor } from './plugins/metrics';
