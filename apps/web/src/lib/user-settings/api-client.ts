/**
 * Server-side API client factory for user settings.
 *
 * Every request in this feature goes through the `@game-guild/client`
 * ApiClient — either via generated modules (when their zod schemas match
 * the API contract) or via the client's own `request` pipeline for the
 * preference endpoints whose generated schemas don't (see
 * `lib/user-settings/actions.ts`).
 */

import { auth, getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';

export function getUserSettingsApiClient() {
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';

  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
    tenant: {
      getTenantId: async () => (await auth().catch(() => null))?.tenantId ?? null,
    },
  });
}

/**
 * Returns the authenticated user's id, or null when the caller is
 * unauthenticated. Never throws — callers degrade gracefully.
 */
export async function getAuthenticatedUserId(): Promise<string | null> {
  const session = await auth().catch(() => null);

  if (!session || typeof session === 'function') return null;

  return session.user?.id ?? null;
}
