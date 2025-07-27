'use server';

import { revalidateTag } from 'next/cache';
import {
  getApiAchievements,
  getApiAchievementsLeaderboard,
  getApiAchievementsStatistics,
  postApiAchievements,
  deleteApiAchievementsByAchievementId,
  getApiAchievementsByAchievementId,
  putApiAchievementsByAchievementId,
  postApiAchievementsByAchievementIdAward,
  postApiAchievementsByAchievementIdBulkAward,
  getApiAchievementsByAchievementIdStatistics,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/auth/utils';
import type {
  GetApiAchievementsData,
  PostApiAchievementsData,
  DeleteApiAchievementsByAchievementIdData,
  GetApiAchievementsByAchievementIdData,
  PutApiAchievementsByAchievementIdData,
  PostApiAchievementsByAchievementIdAwardData,
  PostApiAchievementsByAchievementIdBulkAwardData,
  GetApiAchievementsByAchievementIdStatisticsData,
  GetApiAchievementsLeaderboardData,
  GetApiAchievementsStatisticsData,
} from '@/lib/api/generated/types.gen';

/**
 * Get all achievements with optional filtering
 */
export async function getAchievements(data?: GetApiAchievementsData) {
  const client = await configureAuthenticatedClient();
  return getApiAchievements({
    client,
    ...data,
  });
}

/**
 * Get achievements leaderboard
 */
export async function getAchievementsLeaderboard(data?: GetApiAchievementsLeaderboardData) {
  const client = await configureAuthenticatedClient();
  return getApiAchievementsLeaderboard({
    client,
    ...data,
  });
}

/**
 * Get achievements statistics
 */
export async function getAchievementsStatistics(data?: GetApiAchievementsStatisticsData) {
  const client = await configureAuthenticatedClient();
  return getApiAchievementsStatistics({
    client,
    ...data,
  });
}

/**
 * Create a new achievement
 */
export async function createAchievement(data?: PostApiAchievementsData) {
  const client = await configureAuthenticatedClient();
  const result = await postApiAchievements({
    client,
    ...data,
  });
  
  // Revalidate achievements cache
  revalidateTag('achievements');
  
  return result;
}

/**
 * Delete an achievement by ID
 */
export async function deleteAchievement(data: DeleteApiAchievementsByAchievementIdData) {
  const client = await configureAuthenticatedClient();
  const result = await deleteApiAchievementsByAchievementId({
    client,
    ...data,
  });
  
  // Revalidate achievements cache
  revalidateTag('achievements');
  
  return result;
}

/**
 * Get achievement by ID
 */
export async function getAchievementById(data: GetApiAchievementsByAchievementIdData) {
  const client = await configureAuthenticatedClient();
  return getApiAchievementsByAchievementId({
    client,
    ...data,
  });
}

/**
 * Update an achievement by ID
 */
export async function updateAchievement(data: PutApiAchievementsByAchievementIdData) {
  const client = await configureAuthenticatedClient();
  const result = await putApiAchievementsByAchievementId({
    client,
    ...data,
  });
  
  // Revalidate achievements cache
  revalidateTag('achievements');
  
  return result;
}

/**
 * Award an achievement to a user
 */
export async function awardAchievement(data: PostApiAchievementsByAchievementIdAwardData) {
  const client = await configureAuthenticatedClient();
  const result = await postApiAchievementsByAchievementIdAward({
    client,
    ...data,
  });
  
  // Revalidate achievements and user achievements cache
  revalidateTag('achievements');
  revalidateTag('user-achievements');
  
  return result;
}

/**
 * Award an achievement to multiple users (bulk operation)
 */
export async function bulkAwardAchievement(data: PostApiAchievementsByAchievementIdBulkAwardData) {
  const client = await configureAuthenticatedClient();
  const result = await postApiAchievementsByAchievementIdBulkAward({
    client,
    ...data,
  });
  
  // Revalidate achievements and user achievements cache
  revalidateTag('achievements');
  revalidateTag('user-achievements');
  
  return result;
}

/**
 * Get statistics for a specific achievement
 */
export async function getAchievementStatistics(data: GetApiAchievementsByAchievementIdStatisticsData) {
  const client = await configureAuthenticatedClient();
  return getApiAchievementsByAchievementIdStatistics({
    client,
    ...data,
  });
}
