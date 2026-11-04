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
export { createClient, type ClientConfig } from './client.js';
export { createServerClient, type ServerClientConfig } from './server.js';

// Result types
export type { Result, Ok, Err, ResultData, ResultError } from './runtime/result/types.js';
export { ok, err, isOk, isErr, unwrap, unwrapOr, match } from './runtime/result/helpers.js';

// Error types and guards
export type { ApiError, ApiErrorCode, AuthenticationError, AuthorizationError, NotFoundError, ValidationError } from './runtime/errors/types.js';

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
} from './runtime/errors/guards.js';

// Validation utilities
export { transformZodError, isZodError, safeParse } from './runtime/errors/validation.js';

// DevTools
export { DevTools, type DevToolsConfig } from './runtime/devtools/index.js';

// Deduplication
export { RequestDeduplicator, type DeduplicationConfig } from './runtime/deduplication/index.js';

// Product API exports
export * from './product-exports.js';

// Additional error guard
export { getRetryAfter } from './runtime/errors/guards.js';

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
} from './runtime/auth/types.js';

// Auth utilities
export { encodeJWT, decodeJWT } from './runtime/auth/jwt.js';
export { SessionStore, CsrfStore, CallbackStore, resolveCookieOptions } from './runtime/auth/cookies.js';
export { createCSRFToken, validateCSRFToken } from './runtime/auth/csrf.js';
export { createJWTPayload, toSession, shouldRefreshToken, refreshAccessToken, processSession, encodeSession } from './runtime/auth/session.js';

// Auth errors
export {
  AuthServiceUnavailableError,
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
  MfaVerificationError,
  PasswordResetError,
  EmailVerificationError,
  SessionTerminationError,
  parseErrorBody,
  extractErrorMessage,
  isAuthError,
  isReauthRequired,
  isCredentialsError,
} from './runtime/auth/errors.js';

// Authorization utilities
export { hasRole, hasAllRoles, hasAnyRole, hasPermission, hasAllPermissions, hasAnyPermission, can } from './runtime/auth/authorization.js';

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
} from './runtime/auth/extended-operations.js';
export type {
  MfaVerifyInput,
  MfaSetupResult,
  PasswordResetRequestInput,
  PasswordResetConfirmInput,
  PasswordChangeInput,
  EmailVerificationInput,
  SessionInfo,
} from './runtime/auth/extended-operations.js';

// Auth providers
export { CredentialsProvider } from './runtime/auth/providers/credentials.js';
export { DiscordProvider } from './runtime/auth/providers/discord.js';
export { GitHubProvider } from './runtime/auth/providers/github.js';
export { GoogleProvider } from './runtime/auth/providers/google.js';

// Tenant types
export type { TenantProvider, TenantConfig } from './runtime/tenant/types.js';

// Transport types
export type { RequestConfig, ApiResponse, Interceptor } from './runtime/transport/types.js';

// ─── Next.js Integration ─────────────────────────────────────────
export { GameGuildAuth } from './integrations/next/auth.js';
export { parseCookieHeader, parseBackendAuthResponse } from './integrations/next/handlers.js';
export type { OAuthProviderWithMethods } from './integrations/next/oauth-helpers.js';
export {
  createNextClient,
  createNextAuthTokenProvider,
  createNextTenantProvider,
  createClientFromCookies,
  createRouteClient,
  type NextClientConfig,
} from './integrations/next/index.js';

// ─── Plugins ─────────────────────────────────────────────────────
export { createRetryPlugin, createRetryInterceptor, type RetryConfig } from './plugins/retry.js';
export { createAuthRetryPlugin, type AuthRetryConfig } from './plugins/auth-retry.js';
export { createLoggingInterceptor, type LoggingConfig, type LogLevel, type LoggerFn } from './plugins/logging.js';
export { createCacheInterceptor, MemoryCache, type CacheConfig, type CacheInterceptor } from './plugins/cache.js';
export { createMetricsInterceptor, type MetricsConfig, type RequestMetrics, type AggregatedMetrics, type MetricsInterceptor } from './plugins/metrics.js';
