'use server';

import { auth } from '@/auth';
import { environment } from '@/configs/environment';
import { client } from '@/lib/api/generated/client.gen';

/**
 * Configure the default API client with authentication headers
 * This should be called at the beginning of server actions that need authentication
 */
export async function configureAuthenticatedClient() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Configure the default client with authentication
  client.setConfig({
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.api.accessToken}`,
      'X-Tenant-Id': session.currentTenant?.id,
      'Content-Type': 'application/json',
    },
  });

  return session;
}

/**
 * Get current session data
 */
export async function getAuthenticatedSession() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  return session;
}
