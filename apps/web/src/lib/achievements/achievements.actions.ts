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
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
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
  await configureAuthenticatedClient();
  return getApiAchievements({
    query: data?.query,
  });
}

/**
 * Get achievements leaderboard
 */
export async function getAchievementsLeaderboard(data?: GetApiAchievementsLeaderboardData) {
  await configureAuthenticatedClient();
  return getApiAchievementsLeaderboard({
    query: data?.query,
  });
}

/**
 * Get achievements statistics
 */
export async function getAchievementsStatistics(data?: GetApiAchievementsStatisticsData) {
  await configureAuthenticatedClient();
  return getApiAchievementsStatistics({
    query: data?.query,
  });
}

/**
 * Create a new achievement
 */
export async function createAchievement(data?: PostApiAchievementsData) {
  await configureAuthenticatedClient();
  const result = await postApiAchievements({
    body: data?.body,
  });
  
  // Revalidate achievements cache
  revalidateTag('achievements');
  
  return result;
}

/**
 * Delete an achievement by ID
 */
export async function deleteAchievement(data: DeleteApiAchievementsByAchievementIdData) {
  await configureAuthenticatedClient();
  const result = await deleteApiAchievementsByAchievementId({
    path: data.path,
  });
  
  // Revalidate achievements cache
  revalidateTag('achievements');
  
  return result;
}

/**
 * Get achievement by ID
 */
export async function getAchievementById(data: GetApiAchievementsByAchievementIdData) {
  await configureAuthenticatedClient();
  return getApiAchievementsByAchievementId({
    path: data.path,
  });
}

/**
 * Update an achievement by ID
 */
export async function updateAchievement(data: PutApiAchievementsByAchievementIdData) {
  await configureAuthenticatedClient();
  const result = await putApiAchievementsByAchievementId({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate achievements cache
  revalidateTag('achievements');
  
  return result;
}

/**
 * Award an achievement to a user
 */
export async function awardAchievement(data: PostApiAchievementsByAchievementIdAwardData) {
  await configureAuthenticatedClient();
  const result = await postApiAchievementsByAchievementIdAward({
    path: data.path,
    body: data.body,
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
  await configureAuthenticatedClient();
  const result = await postApiAchievementsByAchievementIdBulkAward({
    path: data.path,
    body: data.body,
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
  await configureAuthenticatedClient();
  return getApiAchievementsByAchievementIdStatistics({
    path: data.path,
  });
}
