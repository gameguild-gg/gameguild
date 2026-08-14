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
 *   baseUrl: 'https://api.gameguild.gg',
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
export { err, isErr, isOk, match, ok, unwrap, unwrapOr } from './runtime/result/helpers.js';
export type { Err, Ok, Result, ResultData, ResultError } from './runtime/result/types.js';

// Error types and guards
export type { ApiError, ApiErrorCode, AuthenticationError, AuthorizationError, NotFoundError, ValidationError } from './runtime/errors/types.js';

export {
  getRequiredFeature, getRequiredPermissions, isApiError, isFeatureNotAvailable, isForbidden,
  isInsufficientPermissions, isNetworkError, isNotFoundError, isRetryableError, isUnauthorized, isValidationError
} from './runtime/errors/guards.js';

// Validation utilities
export { isZodError, safeParse, transformZodError } from './runtime/errors/validation.js';

// DevTools
export { DevTools, type DevToolsConfig } from './runtime/devtools/index.js';

// Deduplication
export { RequestDeduplicator, type DeduplicationConfig } from './runtime/deduplication/index.js';

// Generated types and schemas - export all
export * from './generated/errors.gen.js';
export * from './generated/types.gen.js';
export * as GeneratedApi from './generated/index.js';

// Additional error guard
export { getRetryAfter } from './runtime/errors/guards.js';

// Auth types
export type {
  AuthCallbacks, AuthConfig, AuthInstance, CookieConfig, CredentialsProviderConfig, GameGuildAuthConfig, JWTPayload, OAuthProviderConfig, PagesConfig, Provider, ProviderConfig,
  ProviderResult, ProviderType, ResolvedAuthConfig, Session, SessionProviderProps, SessionStatus, SessionUser, TokenPair, TokenProvider, UseSessionReturn
} from './runtime/auth/types.js';

// Auth utilities
export { CallbackStore, CsrfStore, resolveCookieOptions, SessionStore } from './runtime/auth/cookies.js';
export { createCSRFToken, validateCSRFToken } from './runtime/auth/csrf.js';
export { decodeJWT, encodeJWT } from './runtime/auth/jwt.js';
export { createJWTPayload, encodeSession, processSession, refreshAccessToken, shouldRefreshToken, toSession } from './runtime/auth/session.js';

// Auth errors
export {
  AccountLockedError, AuthError, ConfigError, CredentialsSignInError, CSRFError, EmailVerificationError, extractErrorMessage, InvalidSessionError, isAuthError, isCredentialsError, isReauthRequired, MfaRequiredError, MfaVerificationError, MissingSecretError, OAuthCallbackError, OAuthError, parseErrorBody, PasswordResetError, ProviderNotFoundError, SessionExpiredError, SessionTerminationError, SignUpError, TokenRefreshError
} from './runtime/auth/errors.js';

// Authorization utilities
export { can, hasAllPermissions, hasAllRoles, hasAnyPermission, hasAnyRole, hasPermission, hasRole } from './runtime/auth/authorization.js';

// Extended auth operations (MFA, password reset, email verification, session management)
export {
  changePassword, confirmPasswordReset, getMfaMethods, listSessions, requestPasswordReset, resendVerificationEmail, sendVerificationEmail, setupTotpMfa, terminateAllSessions, terminateOtherSessions, terminateSession, verifyEmail, verifyMfa
} from './runtime/auth/extended-operations.js';
export type {
  EmailVerificationInput, MfaSetupResult, MfaVerifyInput, PasswordChangeInput, PasswordResetConfirmInput, PasswordResetRequestInput, SessionInfo
} from './runtime/auth/extended-operations.js';

// Auth providers
export { CredentialsProvider } from './runtime/auth/providers/credentials.js';
export { GitHubProvider } from './runtime/auth/providers/github.js';
export { GoogleProvider } from './runtime/auth/providers/google.js';
export { DiscordProvider } from './runtime/auth/providers/discord.js';

// Tenant types
export type { TenantConfig, TenantProvider } from './runtime/tenant/types.js';

// Transport types
export type { ApiResponse, Interceptor, RequestConfig } from './runtime/transport/types.js';

// ─── Next.js Integration ─────────────────────────────────────────
export { GameGuildAuth } from './integrations/next/auth.js';
export { parseBackendAuthResponse, parseCookieHeader } from './integrations/next/handlers.js';
export {
  createClientFromCookies, createNextAuthTokenProvider, createNextClient, createNextTenantProvider, createRouteClient,
  type NextClientConfig
} from './integrations/next/index.js';
export type { OAuthProviderWithMethods } from './integrations/next/oauth-helpers.js';

// ─── Plugins ─────────────────────────────────────────────────────
export { createAuthRetryPlugin, type AuthRetryConfig } from './plugins/auth-retry.js';
export { createCacheInterceptor, MemoryCache, type CacheConfig, type CacheInterceptor } from './plugins/cache.js';
export { createLoggingInterceptor, type LoggerFn, type LoggingConfig, type LogLevel } from './plugins/logging.js';
export { createMetricsInterceptor, type AggregatedMetrics, type MetricsConfig, type MetricsInterceptor, type RequestMetrics } from './plugins/metrics.js';
export { createRetryInterceptor, createRetryPlugin, type RetryConfig } from './plugins/retry.js';

