/**
 * Next.js Integration
 *
 * Utilities for integrating the API client with Next.js applications.
 */

import { createServerClient, type ServerClientConfig } from '../../server.js';
import type { ApiClient } from '../../runtime/client.js';
import type { TokenProvider } from '../../runtime/auth/types.js';
import type { TenantProvider } from '../../runtime/tenant/types.js';

export type { ServerClientConfig };

/**
 * Configuration for Next.js API client
 */
export interface NextClientConfig extends Omit<ServerClientConfig, 'auth' | 'tenant'> {
  /**
   * Function to get auth session from NextAuth
   * This should return the session with accessToken
   */
  getSession?: () => Promise<{ accessToken?: string; refreshToken?: string } | null>;

  /**
   * Function to get tenant ID
   * Can read from headers, cookies, or session
   */
  getTenantId?: () => Promise<string | null>;

  /**
   * Header name for tenant ID
   * @default 'X-Tenant-Id'
   */
  tenantHeader?: string;
}

/**
 * Create a token provider from NextAuth session
 */
export function createNextAuthTokenProvider(
  getSession: NonNullable<NextClientConfig['getSession']>
): TokenProvider {
  return {
    async getAccessToken() {
      const session = await getSession();
      return session?.accessToken ?? null;
    },
    async getRefreshToken() {
      const session = await getSession();
      return session?.refreshToken ?? null;
    },
    onAuthenticationRequired: async () => {
      // In server context, we typically let the middleware handle redirects
      // This callback is for notification purposes
      console.warn('[api-client] Authentication required but no session available');
    },
  };
}

/**
 * Create a tenant provider for Next.js
 */
export function createNextTenantProvider(
  getTenantId: NonNullable<NextClientConfig['getTenantId']>
): TenantProvider {
  return {
    getTenantId,
    onTenantRequired: async () => {
      console.warn('[api-client] Tenant ID required but not available');
    },
  };
}

/**
 * Create an API client configured for Next.js server-side usage
 *
 * @example
 * ```typescript
 * // In a Server Component or Server Action
 * import { createNextClient } from '@game-guild/api-client/next';
 * import { auth } from '@/auth';
 *
 * export async function getUser(id: string) {
 *   const client = createNextClient({
 *     baseUrl: process.env.API_URL!,
 *     getSession: () => auth(),
 *   });
 *
 *   return await client.users.get(id);
 * }
 * ```
 */
export function createNextClient(config: NextClientConfig): ApiClient {
  const serverConfig: ServerClientConfig = {
    baseUrl: config.baseUrl,
    timeout: config.timeout,
    interceptors: config.interceptors,
  };

  // Add auth if session getter is provided
  if (config.getSession) {
    serverConfig.auth = createNextAuthTokenProvider(config.getSession);
  }

  // Add tenant if getter is provided
  if (config.getTenantId) {
    serverConfig.tenant = createNextTenantProvider(config.getTenantId);
  }

  return createServerClient(serverConfig);
}

/**
 * Helper to create a client with cookies-based auth
 * Useful for reading auth state from cookies() in RSC/Server Actions
 */
export async function createClientFromCookies(
  config: Omit<NextClientConfig, 'getSession'> & {
    /** Cookie name for access token */
    accessTokenCookie?: string;
    /** Cookie name for tenant ID */
    tenantCookie?: string;
    /** Function to get cookies - typically `cookies()` from next/headers */
    getCookies: () => Promise<{
      get: (name: string) => { value: string } | undefined;
    }>;
  }
): Promise<ApiClient> {
  const cookies = await config.getCookies();

  const accessTokenCookieName = config.accessTokenCookie || 'access_token';
  const tenantCookieName = config.tenantCookie || 'tenant_id';

  return createNextClient({
    ...config,
    getSession: async () => {
      const token = cookies.get(accessTokenCookieName);
      return token ? { accessToken: token.value } : null;
    },
    getTenantId: async () => {
      const tenant = cookies.get(tenantCookieName);
      return tenant?.value ?? null;
    },
  });
}

/**
 * Create a client configured for use in API Routes or Route Handlers
 */
export function createRouteClient(
  config: NextClientConfig & {
    /** Request headers to extract auth/tenant from */
    headers?: Headers;
  }
): ApiClient {
  let effectiveConfig = { ...config };

  if (config.headers) {
    // Extract auth from Authorization header if not using session getter
    if (!config.getSession) {
      const authHeader = config.headers.get('Authorization');
      if (authHeader?.startsWith('Bearer ')) {
        const token = authHeader.slice(7);
        effectiveConfig.getSession = async () => ({ accessToken: token });
      }
    }

    // Extract tenant from header if not using tenant getter
    if (!config.getTenantId) {
      const tenantHeader = config.tenantHeader || 'X-Tenant-Id';
      const tenantId = config.headers.get(tenantHeader);
      if (tenantId) {
        effectiveConfig.getTenantId = async () => tenantId;
      }
    }
  }

  return createNextClient(effectiveConfig);
}
