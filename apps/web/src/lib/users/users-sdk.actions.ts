'use server';

import { revalidateTag } from 'next/cache';
import {
  getApiUsers,
  postApiUsers,
  deleteApiUsersById,
  getApiUsersById,
  putApiUsersById,
  postApiUsersByIdRestore,
  putApiUsersByIdBalance,
  getApiUsersStatistics,
  getApiUsersSearch,
  postApiUsersBulk,
  patchApiUsersBulkActivate,
  patchApiUsersBulkDeactivate,
  getApiUsersByUserIdAchievements,
  getApiUsersByUserIdAchievementsProgress,
  getApiUsersByUserIdAchievementsSummary,
  getApiUsersByUserIdAchievementsAvailable,
  postApiUsersByUserIdAchievementsByAchievementIdProgress,
  getApiUsersByUserIdAchievementsByAchievementIdPrerequisites,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  GetApiUsersData,
  PostApiUsersData,
  DeleteApiUsersByIdData,
  GetApiUsersByIdData,
  PutApiUsersByIdData,
  PostApiUsersByIdRestoreData,
  PutApiUsersByIdBalanceData,
  GetApiUsersStatisticsData,
  GetApiUsersSearchData,
  PostApiUsersBulkData,
  PatchApiUsersBulkActivateData,
  PatchApiUsersBulkDeactivateData,
  GetApiUsersByUserIdAchievementsData,
  GetApiUsersByUserIdAchievementsProgressData,
  GetApiUsersByUserIdAchievementsSummaryData,
  GetApiUsersByUserIdAchievementsAvailableData,
  PostApiUsersByUserIdAchievementsByAchievementIdProgressData,
  GetApiUsersByUserIdAchievementsByAchievementIdPrerequisitesData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// USER MANAGEMENT
// =============================================================================

/**
 * Get all users with optional filtering
 */
export async function getUsers(data?: GetApiUsersData) {
  await configureAuthenticatedClient();
  
  return getApiUsers({
    query: data?.query,
  });
}

/**
 * Create a new user
 */
export async function createUser(data?: PostApiUsersData) {
  await configureAuthenticatedClient();
  
  const result = await postApiUsers({
    body: data?.body,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

/**
 * Delete a user by ID
 */
export async function deleteUser(data: DeleteApiUsersByIdData) {
  await configureAuthenticatedClient();
  
  const result = await deleteApiUsersById({
    path: data.path,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

/**
 * Get a specific user by ID
 */
export async function getUserById(data: GetApiUsersByIdData) {
  await configureAuthenticatedClient();
  
  return getApiUsersById({
    path: data.path,
  });
}

/**
 * Update a user by ID
 */
export async function updateUser(data: PutApiUsersByIdData) {
  await configureAuthenticatedClient();
  
  const result = await putApiUsersById({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

/**
 * Restore a deleted user
 */
export async function restoreUser(data: PostApiUsersByIdRestoreData) {
  await configureAuthenticatedClient();
  
  const result = await postApiUsersByIdRestore({
    path: data.path,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

/**
 * Update user balance
 */
export async function updateUserBalance(data: PutApiUsersByIdBalanceData) {
  await configureAuthenticatedClient();
  
  const result = await putApiUsersByIdBalance({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

// =============================================================================
// USER ANALYTICS & SEARCH
// =============================================================================

/**
 * Get user statistics
 */
export async function getUserStatistics(data?: GetApiUsersStatisticsData) {
  await configureAuthenticatedClient();
  
  return getApiUsersStatistics({
    query: data?.query,
  });
}

/**
 * Search users
 */
export async function searchUsers(data?: GetApiUsersSearchData) {
  await configureAuthenticatedClient();
  
  return getApiUsersSearch({
    query: data?.query,
  });
}

// =============================================================================
// BULK USER OPERATIONS
// =============================================================================

/**
 * Create users in bulk
 */
export async function createUsersBulk(data?: PostApiUsersBulkData) {
  await configureAuthenticatedClient();
  
  const result = await postApiUsersBulk({
    body: data?.body,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

/**
 * Activate users in bulk
 */
export async function activateUsersBulk(data?: PatchApiUsersBulkActivateData) {
  await configureAuthenticatedClient();
  
  const result = await patchApiUsersBulkActivate({
    body: data?.body,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

/**
 * Deactivate users in bulk
 */
export async function deactivateUsersBulk(data?: PatchApiUsersBulkDeactivateData) {
  await configureAuthenticatedClient();
  
  const result = await patchApiUsersBulkDeactivate({
    body: data?.body,
  });
  
  // Revalidate users cache
  revalidateTag('users');
  
  return result;
}

// =============================================================================
// USER ACHIEVEMENTS INTEGRATION
// =============================================================================

/**
 * Get user achievements
 */
export async function getUserAchievements(data: GetApiUsersByUserIdAchievementsData) {
  await configureAuthenticatedClient();
  
  return getApiUsersByUserIdAchievements({
    path: data.path,
  });
}

/**
 * Get user achievement progress
 */
export async function getUserAchievementProgress(data: GetApiUsersByUserIdAchievementsProgressData) {
  await configureAuthenticatedClient();
  
  return getApiUsersByUserIdAchievementsProgress({
    path: data.path,
  });
}

/**
 * Get user achievement summary
 */
export async function getUserAchievementSummary(data: GetApiUsersByUserIdAchievementsSummaryData) {
  await configureAuthenticatedClient();
  
  return getApiUsersByUserIdAchievementsSummary({
    path: data.path,
  });
}

/**
 * Get available achievements for user
 */
export async function getUserAvailableAchievements(data: GetApiUsersByUserIdAchievementsAvailableData) {
  await configureAuthenticatedClient();
  
  return getApiUsersByUserIdAchievementsAvailable({
    path: data.path,
  });
}

/**
 * Update user achievement progress
 */
export async function updateUserAchievementProgress(data: PostApiUsersByUserIdAchievementsByAchievementIdProgressData) {
  await configureAuthenticatedClient();
  
  const result = await postApiUsersByUserIdAchievementsByAchievementIdProgress({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate user achievements cache
  revalidateTag('user-achievements');
  
  return result;
}

/**
 * Get achievement prerequisites for user
 */
export async function getUserAchievementPrerequisites(data: GetApiUsersByUserIdAchievementsByAchievementIdPrerequisitesData) {
  await configureAuthenticatedClient();
  
  return getApiUsersByUserIdAchievementsByAchievementIdPrerequisites({
    path: data.path,
  });
}
