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
export type { Result, Ok, Err, ResultData, ResultError } from './runtime/result/types.js';
export { ok, err, isOk, isErr, unwrap, unwrapOr, match } from './runtime/result/helpers.js';

// Error types and guards
export type {
  ApiError,
  ApiErrorCode,
  AuthenticationError,
  AuthorizationError,
  NotFoundError,
  ValidationError,
} from './runtime/errors/types.js';

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

// Generated types and schemas - export all
export * from './generated/types.gen.js';
export * from './generated/errors.gen.js';

// Additional error guard
export { getRetryAfter } from './runtime/errors/guards.js';

// Auth types
export type { TokenProvider, TokenPair, AuthConfig } from './runtime/auth/types.js';

// Tenant types
export type { TenantProvider, TenantConfig } from './runtime/tenant/types.js';

// Transport types
export type { RequestConfig, ApiResponse, Interceptor } from './runtime/transport/types.js';
