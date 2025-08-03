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

  // Debug logging for session state
  console.log('Session token exists:', !!session?.api?.accessToken);
  console.log('Current tenant ID:', session?.currentTenant?.id);

  if (!session?.api?.accessToken) {
    if (session?.error) {
      console.error('Session error detected:', session.error);
      throw new Error(`Authentication error: ${session.error}`);
    }
    throw new Error('Authentication required - no access token');
  }

  // Debug logging
  console.log('Configuring client with baseUrl:', environment.apiBaseUrl);

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
