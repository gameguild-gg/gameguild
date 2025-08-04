'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getHealth, getHealthDatabase } from '@/lib/api/generated/sdk.gen';

import type { GetHealthData, GetHealthDatabaseData } from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

/**
 * Get overall system health status
 */
export async function getSystemHealthAction(params?: GetHealthData) {
  await configureAuthenticatedClient();
  const result = await getHealth({
    ...params,
  });

  revalidateTag('system-health');
  return result;
}

/**
 * Get database health status
 */
export async function getDatabaseHealthAction(params?: GetHealthDatabaseData) {
  await configureAuthenticatedClient();
  const result = await getHealthDatabase({
    ...params,
  });

  revalidateTag('system-health');
  revalidateTag('database-health');
  return result;
}
