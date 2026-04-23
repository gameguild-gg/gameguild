/**
 * React Integration
 *
 * Hooks and context for using the API client in React components.
 */

// Note: This file uses React 18+ features
// The actual implementation requires React as a peer dependency

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';

// Export React Query hooks
export * from './query-hooks.js';

// Export Auth components and hooks
export { SessionProvider } from './session-provider.js';
export { SessionContext } from './session-provider.js';
export type { SessionContextValue } from './session-provider.js';
export { useSession } from './use-session.js';
export { useAuth } from './use-auth.js';
export { createAuthBroadcast } from './broadcast.js';
export type { AuthBroadcastMessage } from './broadcast.js';

/**
 * Query state for async operations
 */
export interface QueryState<T> {
  data: T | undefined;
  error: ApiError | undefined;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
}

/**
 * Mutation state for async operations
 */
export interface MutationState<T, TVariables> {
  data: T | undefined;
  error: ApiError | undefined;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  mutate: (variables: TVariables) => Promise<Result<T, ApiError>>;
  reset: () => void;
}

/**
 * Options for useQuery hook
 */
export interface UseQueryOptions<T> {
  /** Enable/disable the query */
  enabled?: boolean;
  /** Refetch interval in ms */
  refetchInterval?: number;
  /** Refetch on window focus */
  refetchOnWindowFocus?: boolean;
  /** Retry count on failure */
  retry?: number | boolean;
  /** Callback on success */
  onSuccess?: (data: T) => void;
  /** Callback on error */
  onError?: (error: ApiError) => void;
}

/**
 * Options for useMutation hook
 */
export interface UseMutationOptions<T, TVariables> {
  /** Callback on success */
  onSuccess?: (data: T, variables: TVariables) => void;
  /** Callback on error */
  onError?: (error: ApiError, variables: TVariables) => void;
  /** Callback on settle (success or error) */
  onSettled?: (data: T | undefined, error: ApiError | undefined, variables: TVariables) => void;
}

// Re-export types for consumers
export type { ApiClient, Result, ApiError };

/**
 * The actual hook implementations require React.
 * These are type-only exports for documentation purposes.
 *
 * Usage example:
 * ```tsx
 * import { ApiClientProvider, useApiClient, useQuery, useMutation } from '@game-guild/client/react';
 *
 * // Wrap your app with the provider
 * function App() {
 *   const client = createClient({ baseUrl: 'https://api.example.com' });
 *   return (
 *     <ApiClientProvider client={client}>
 *       <MyComponent />
 *     </ApiClientProvider>
 *   );
 * }
 *
 * // Use hooks in components
 * function MyComponent() {
 *   const client = useApiClient();
 *   const { data, isLoading, error } = useQuery(
 *     ['user', userId],
 *     () => client.users.get(userId)
 *   );
 *
 *   if (isLoading) return <Loading />;
 *   if (error) return <Error error={error} />;
 *   return <User data={data} />;
 * }
 * ```
 */

// ============================================================================
// Context and Provider (stub - requires React)
// ============================================================================

/**
 * React context for the API client
 * @internal
 */
export const ApiClientContext = {
  displayName: 'ApiClientContext',
} as const;

/**
 * Props for ApiClientProvider
 */
export interface ApiClientProviderProps {
  client: ApiClient;
  children: unknown; // React.ReactNode when React is available
}

// ============================================================================
// Hook implementations (require React runtime)
// ============================================================================

// These are placeholder exports that will be implemented when React is available
// as a peer dependency at runtime.

/**
 * Hook to access the API client from context
 * @throws If used outside of ApiClientProvider
 */
export function useApiClient(): ApiClient {
  throw new Error('useApiClient requires React. Make sure you have React installed as a peer dependency.');
}

/**
 * Hook for data fetching with caching and automatic refetching
 *
 * @param queryKey - Unique key for the query (used for caching)
 * @param queryFn - Function that returns a Promise<Result<T, ApiError>>
 * @param options - Query options
 */
export function useQuery<T>(_queryKey: unknown[], _queryFn: () => Promise<Result<T, ApiError>>, _options?: UseQueryOptions<T>): QueryState<T> {
  throw new Error('useQuery requires React. Make sure you have React installed as a peer dependency.');
}

/**
 * Hook for data mutations (create, update, delete)
 *
 * @param mutationFn - Function that performs the mutation
 * @param options - Mutation options
 */
export function useMutation<T, TVariables>(
  _mutationFn: (variables: TVariables) => Promise<Result<T, ApiError>>,
  _options?: UseMutationOptions<T, TVariables>,
): MutationState<T, TVariables> {
  throw new Error('useMutation requires React. Make sure you have React installed as a peer dependency.');
}

/**
 * Hook for optimistic updates
 *
 * @param queryKey - Key of the query to update optimistically
 */
export function useOptimisticUpdate<T>(_queryKey: unknown[]): {
  update: (updater: (old: T | undefined) => T) => void;
  rollback: () => void;
} {
  throw new Error('useOptimisticUpdate requires React. Make sure you have React installed as a peer dependency.');
}
