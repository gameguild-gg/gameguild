'use server';

import { auth } from '@/auth';
import { environment } from '@/configs/environment';
import { getTestingRequests } from '@/lib/api/generated';
import { createClient } from '@/lib/api/generated/client';

/**
 * Alternative Approach: Create a custom authenticated client for each request
 * This is useful when you don't want to modify the global client
 */

// Option 2: Create custom client per request
export async function getTestingRequestsWithCustomClient() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  // Create a custom client for this request
  const authenticatedClient = createClient({
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.api.accessToken}`,
      'X-Tenant-Id': session.currentTenant?.id,
      'Content-Type': 'application/json',
    },
  });

  try {
    // Pass the custom client to the SDK function
    const response = await getTestingRequests({
      client: authenticatedClient,
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching testing requests:', error);
    throw new Error('Failed to fetch testing requests');
  }
}

// Option 3: Direct options approach (like your current testing lab actions)
export async function getTestingRequestsWithOptions() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    // Pass options directly to the SDK function (your current approach)
    const response = await getTestingRequests({
      baseUrl: environment.apiBaseUrl,
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'X-Tenant-Id': session.currentTenant?.id,
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching testing requests:', error);
    throw new Error('Failed to fetch testing requests');
  }
}

// Option 4: Utility function that returns configured options
export async function getAuthenticatedRequestOptions() {
  const session = await auth();

  if (!session?.api.accessToken) {
    throw new Error('Authentication required');
  }

  return {
    baseUrl: environment.apiBaseUrl,
    headers: {
      Authorization: `Bearer ${session.api.accessToken}`,
      'X-Tenant-Id': session.currentTenant?.id,
      'Content-Type': 'application/json',
    },
  };
}

// Use the utility function
export async function getTestingRequestsWithUtility() {
  const options = await getAuthenticatedRequestOptions();

  try {
    const response = await getTestingRequests(options);
    return response.data || [];
  } catch (error) {
    console.error('Error fetching testing requests:', error);
    throw new Error('Failed to fetch testing requests');
  }
}
