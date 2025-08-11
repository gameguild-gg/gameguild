'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteApiTenantDomainsById,
  deleteApiTenantDomainsUserGroupsById,
  deleteApiTenantDomainsUserGroupsMemberships,
  getApiTenantDomains,
  getApiTenantDomainsById,
  getApiTenantDomainsDomainMatch,
  getApiTenantDomainsGroupsByGroupIdUsers,
  getApiTenantDomainsMembershipsUserByUserId,
  getApiTenantDomainsUserGroups,
  getApiTenantDomainsUserGroupsByGroupIdMembers,
  getApiTenantDomainsUserGroupsById,
  getApiTenantDomainsUsersByUserIdGroups,
  postApiTenantDomains,
  postApiTenantDomainsAutoAssign,
  postApiTenantDomainsAutoAssignBulk,
  postApiTenantDomainsByTenantIdSetMainByDomainId,
  postApiTenantDomainsMemberships,
  postApiTenantDomainsUserGroups,
  postApiTenantDomainsUserGroupsMemberships,
  putApiTenantDomainsById,
  putApiTenantDomainsUserGroupsById,
} from '@/lib/api/generated/sdk.gen';
import type {
  DeleteApiTenantDomainsByIdData,
  DeleteApiTenantDomainsUserGroupsByIdData,
  DeleteApiTenantDomainsUserGroupsMembershipsData,
  GetApiTenantDomainsByIdData,
  GetApiTenantDomainsData,
  GetApiTenantDomainsDomainMatchData,
  GetApiTenantDomainsGroupsByGroupIdUsersData,
  GetApiTenantDomainsMembershipsUserByUserIdData,
  GetApiTenantDomainsUserGroupsByGroupIdMembersData,
  GetApiTenantDomainsUserGroupsByIdData,
  GetApiTenantDomainsUserGroupsData,
  GetApiTenantDomainsUsersByUserIdGroupsData,
  PostApiTenantDomainsAutoAssignBulkData,
  PostApiTenantDomainsAutoAssignData,
  PostApiTenantDomainsByTenantIdSetMainByDomainIdData,
  PostApiTenantDomainsData,
  PostApiTenantDomainsMembershipsData,
  PostApiTenantDomainsUserGroupsData,
  PostApiTenantDomainsUserGroupsMembershipsData,
  PutApiTenantDomainsByIdData,
  PutApiTenantDomainsUserGroupsByIdData,
} from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// TENANT DOMAIN MANAGEMENT
// =============================================================================

/**
 * Get all tenant domains
 */
export async function getTenantDomains(data?: GetApiTenantDomainsData) {
  await configureAuthenticatedClient();

  return getApiTenantDomains({
    query: data?.query,
  });
}

/**
 * Create a new tenant domain
 */
export async function createTenantDomain(data?: PostApiTenantDomainsData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomains({
    body: data?.body,
  });

  // Revalidate tenant domains cache
  revalidateTag('tenant-domains');

  return result;
}

/**
 * Delete a tenant domain by ID
 */
export async function deleteTenantDomain(data: DeleteApiTenantDomainsByIdData) {
  await configureAuthenticatedClient();

  const result = await deleteApiTenantDomainsById({
    path: data.path,
  });

  // Revalidate tenant domains cache
  revalidateTag('tenant-domains');

  return result;
}

/**
 * Get a specific tenant domain by ID
 */
export async function getTenantDomainById(data: GetApiTenantDomainsByIdData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsById({
    path: data.path,
  });
}

/**
 * Update a tenant domain by ID
 */
export async function updateTenantDomain(data: PutApiTenantDomainsByIdData) {
  await configureAuthenticatedClient();

  const result = await putApiTenantDomainsById({
    path: data.path,
    body: data.body,
  });

  // Revalidate tenant domains cache
  revalidateTag('tenant-domains');

  return result;
}

/**
 * Set main domain for a tenant
 */
export async function setMainTenantDomain(data: PostApiTenantDomainsByTenantIdSetMainByDomainIdData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomainsByTenantIdSetMainByDomainId({
    path: data.path,
  });

  // Revalidate tenant domains cache
  revalidateTag('tenant-domains');

  return result;
}

// =============================================================================
// TENANT USER GROUP MANAGEMENT
// =============================================================================

/**
 * Get tenant user groups
 */
export async function getTenantUserGroups(data?: GetApiTenantDomainsUserGroupsData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsUserGroups({
    query: data?.query,
  });
}

/**
 * Create a new tenant user group
 */
export async function createTenantUserGroup(data?: PostApiTenantDomainsUserGroupsData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomainsUserGroups({
    body: data?.body,
  });

  // Revalidate tenant user groups cache
  revalidateTag('tenant-user-groups');

  return result;
}

/**
 * Delete a tenant user group by ID
 */
export async function deleteTenantUserGroup(data: DeleteApiTenantDomainsUserGroupsByIdData) {
  await configureAuthenticatedClient();

  const result = await deleteApiTenantDomainsUserGroupsById({
    path: data.path,
  });

  // Revalidate tenant user groups cache
  revalidateTag('tenant-user-groups');

  return result;
}

/**
 * Get a specific tenant user group by ID
 */
export async function getTenantUserGroupById(data: GetApiTenantDomainsUserGroupsByIdData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsUserGroupsById({
    path: data.path,
  });
}

/**
 * Update a tenant user group by ID
 */
export async function updateTenantUserGroup(data: PutApiTenantDomainsUserGroupsByIdData) {
  await configureAuthenticatedClient();

  const result = await putApiTenantDomainsUserGroupsById({
    path: data.path,
    body: data.body,
  });

  // Revalidate tenant user groups cache
  revalidateTag('tenant-user-groups');

  return result;
}

// =============================================================================
// TENANT USER GROUP MEMBERSHIP MANAGEMENT
// =============================================================================

/**
 * Get user memberships in tenant groups
 */
export async function getTenantUserMemberships(data: GetApiTenantDomainsMembershipsUserByUserIdData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsMembershipsUserByUserId({
    path: data.path,
  });
}

/**
 * Remove user from tenant user groups
 */
export async function removeTenantUserGroupMembership(data: DeleteApiTenantDomainsUserGroupsMembershipsData) {
  await configureAuthenticatedClient();

  const result = await deleteApiTenantDomainsUserGroupsMemberships({
    query: data.query,
  });

  // Revalidate tenant memberships cache
  revalidateTag('tenant-memberships');

  return result;
}

/**
 * Add user to tenant user group
 */
export async function addTenantUserGroupMembership(data?: PostApiTenantDomainsUserGroupsMembershipsData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomainsUserGroupsMemberships({
    body: data?.body,
  });

  // Revalidate tenant memberships cache
  revalidateTag('tenant-memberships');

  return result;
}

/**
 * Get members of a specific tenant user group
 */
export async function getTenantUserGroupMembers(data: GetApiTenantDomainsUserGroupsByGroupIdMembersData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsUserGroupsByGroupIdMembers({
    path: data.path,
  });
}

/**
 * Get tenant user groups for a specific user
 */
export async function getTenantUserGroupsByUser(data: GetApiTenantDomainsUsersByUserIdGroupsData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsUsersByUserIdGroups({
    path: data.path,
  });
}

/**
 * Get users in a specific tenant group
 */
export async function getTenantGroupUsers(data: GetApiTenantDomainsGroupsByGroupIdUsersData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsGroupsByGroupIdUsers({
    path: data.path,
  });
}

// =============================================================================
// TENANT AUTO-ASSIGNMENT
// =============================================================================

/**
 * Auto-assign user to tenant groups based on domain
 */
export async function autoAssignTenantUser(data?: PostApiTenantDomainsAutoAssignData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomainsAutoAssign({
    body: data?.body,
  });

  // Revalidate tenant memberships cache
  revalidateTag('tenant-memberships');

  return result;
}

/**
 * Bulk auto-assign users to tenant groups
 */
export async function bulkAutoAssignTenantUsers(data?: PostApiTenantDomainsAutoAssignBulkData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomainsAutoAssignBulk({
    body: data?.body,
  });

  // Revalidate tenant memberships cache
  revalidateTag('tenant-memberships');

  return result;
}

/**
 * Find matching domain for tenant assignment
 */
export async function getTenantDomainMatch(data?: GetApiTenantDomainsDomainMatchData) {
  await configureAuthenticatedClient();

  return getApiTenantDomainsDomainMatch({
    query: data?.query,
  });
}

/**
 * Create tenant membership
 */
export async function createTenantMembership(data?: PostApiTenantDomainsMembershipsData) {
  await configureAuthenticatedClient();

  const result = await postApiTenantDomainsMemberships({
    body: data?.body,
  });

  // Revalidate tenant memberships cache
  revalidateTag('tenant-memberships');

  return result;
}
