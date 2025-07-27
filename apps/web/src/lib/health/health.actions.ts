'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getHealth, getHealthDatabase } from '@/lib/api/generated/sdk.gen';

// Get general health status
export async function getHealthData() {
  await configureAuthenticatedClient();

  try {
    const result = await getHealth();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get health status:', error);
    return { success: false, error: 'Failed to get health status' };
  }
}

// Get database health status
export async function getDatabaseHealthData() {
  await configureAuthenticatedClient();

  try {
    const result = await getHealthDatabase();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get database health status:', error);
    return { success: false, error: 'Failed to get database health status' };
  }
}
