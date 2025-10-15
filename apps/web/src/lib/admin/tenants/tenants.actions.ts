'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteApiTenantsById,
  deleteApiTenantsByIdPermanent,
  getApiTenants,
  getApiTenantsActive,
  getApiTenantsById,
  getApiTenantsByNameByName,
  getApiTenantsBySlugBySlug,
  getApiTenantsDeleted,
  getApiTenantsSearch,
  getApiTenantsStatistics,
  postApiTenants,
  postApiTenantsBulkDelete,
  postApiTenantsBulkRestore,
  postApiTenantsByIdActivate,
  postApiTenantsByIdDeactivate,
  postApiTenantsByIdRestore,
  putApiTenantsById,
} from '@/lib/api/generated/sdk.gen';

import type {
  DeleteApiTenantsByIdData,
  DeleteApiTenantsByIdPermanentData,
  GetApiTenantsActiveData,
  GetApiTenantsByIdData,
  GetApiTenantsByNameByNameData,
  GetApiTenantsBySlugBySlugData,
  GetApiTenantsData,
  GetApiTenantsDeletedData,
  GetApiTenantsSearchData,
  GetApiTenantsStatisticsData,
  PostApiTenantsBulkDeleteData,
  PostApiTenantsBulkRestoreData,
  PostApiTenantsByIdActivateData,
  PostApiTenantsByIdDeactivateData,
  PostApiTenantsByIdRestoreData,
  PostApiTenantsData,
  PutApiTenantsByIdData,
} from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

/**
 * Get all tenants
 */
export async function getTenantsAction(params?: GetApiTenantsData) {
  await configureAuthenticatedClient();
  const result = await getApiTenants({
    ...params,
  });

  return result;
}

/**
 * Create a new tenant
 */
export async function createTenantAction(data: Omit<PostApiTenantsData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await postApiTenants({
    body: data.body,
  });

  revalidateTag('tenants');
  return result;
}

/**
 * Delete a tenant
 */
export async function deleteTenantAction(data: Omit<DeleteApiTenantsByIdData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await deleteApiTenantsById({
    path: { id: data.path.id },
  });

  revalidateTag('tenants');
  revalidateTag(`tenant-${data.path.id}`);
  return result;
}

/**
 * Get a tenant by ID
 */
export async function getTenantByIdAction(data: Omit<GetApiTenantsByIdData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsById({
    path: { id: data.path.id },
  });

  return result;
}

/**
 * Update a tenant
 */
export async function updateTenantAction(data: Omit<PutApiTenantsByIdData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await putApiTenantsById({
    path: { id: data.path.id },
    body: data.body,
  });

  revalidateTag('tenants');
  revalidateTag(`tenant-${data.path.id}`);
  return result;
}

/**
 * Get a tenant by name
 */
export async function getTenantByNameAction(data: GetApiTenantsByNameByNameData) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsByNameByName({
    path: { name: data.path.name },
  });

  return result;
}

/**
 * Get a tenant by slug
 */
export async function getTenantBySlugAction(data: GetApiTenantsBySlugBySlugData) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsBySlugBySlug({
    path: { slug: data.path.slug },
  });

  return result;
}

/**
 * Get deleted tenants
 */
export async function getDeletedTenantsAction(params?: GetApiTenantsDeletedData) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsDeleted({
    ...params,
  });

  return result;
}

/**
 * Get active tenants
 */
export async function getActiveTenantsAction(params?: GetApiTenantsActiveData) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsActive({
    ...params,
  });

  return result;
}

/**
 * Search tenants
 */
export async function searchTenantsAction(params?: Omit<GetApiTenantsSearchData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsSearch({
    ...params,
  });

  return result;
}

/**
 * Get tenant statistics
 */
export async function getTenantStatisticsAction(params?: Omit<GetApiTenantsStatisticsData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await getApiTenantsStatistics({
    ...params,
  });

  return result;
}

/**
 * Restore a deleted tenant
 */
export async function restoreTenantAction(data: Omit<PostApiTenantsByIdRestoreData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await postApiTenantsByIdRestore({
    path: { id: data.path.id },
  });

  revalidateTag('tenants');
  revalidateTag('tenants-deleted');
  revalidateTag(`tenant-${data.path.id}`);
  return result;
}

/**
 * Permanently delete a tenant
 */
export async function permanentDeleteTenantAction(data: Omit<DeleteApiTenantsByIdPermanentData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await deleteApiTenantsByIdPermanent({
    path: { id: data.path.id },
  });

  revalidateTag('tenants');
  revalidateTag('tenants-deleted');
  revalidateTag(`tenant-${data.path.id}`);
  return result;
}

/**
 * Activate a tenant
 */
export async function activateTenantAction(data: Omit<PostApiTenantsByIdActivateData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await postApiTenantsByIdActivate({
    path: { id: data.path.id },
  });

  revalidateTag('tenants');
  revalidateTag('tenants-active');
  revalidateTag(`tenant-${data.path.id}`);
  return result;
}

/**
 * Deactivate a tenant
 */
export async function deactivateTenantAction(data: Omit<PostApiTenantsByIdDeactivateData, 'url'>) {
  await configureAuthenticatedClient();
  const result = await postApiTenantsByIdDeactivate({
    path: { id: data.path.id },
  });

  revalidateTag('tenants');
  revalidateTag('tenants-active');
  revalidateTag(`tenant-${data.path.id}`);
  return result;
}

/**
 * Bulk delete tenants
 */
export async function bulkDeleteTenantsAction(data?: PostApiTenantsBulkDeleteData) {
  await configureAuthenticatedClient();
  const result = await postApiTenantsBulkDelete({
    body: data?.body,
  });

  revalidateTag('tenants');
  revalidateTag('tenants-deleted');
  return result;
}

/**
 * Bulk restore tenants
 */
export async function bulkRestoreTenantsAction(data?: PostApiTenantsBulkRestoreData) {
  await configureAuthenticatedClient();
  const result = await postApiTenantsBulkRestore({
    body: data?.body,
  });

  revalidateTag('tenants');
  revalidateTag('tenants-deleted');
  return result;
}
