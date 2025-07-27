'use server';

import { revalidateTag } from 'next/cache';
import {
  getApiProgram,
  postApiProgram,
  deleteApiProgramById,
  getApiProgramById,
  putApiProgramById,
  getApiProgramPublished,
  getApiProgramCategoryByCategory,
  getApiProgramDifficultyByDifficulty,
  getApiProgramCreatorByCreatorId,
  getApiProgramPopular,
  getApiProgramRecent,
  getApiProgramSearch,
  getApiProgramSlugBySlug,
  getApiProgramByIdWithContent,
  postApiProgramByIdClone,
  postApiProgramByIdContent,
  deleteApiProgramByIdContentByContentId,
  putApiProgramByIdContentByContentId,
  postApiProgramByIdContentReorder,
  deleteApiProgramByIdUsersByUserId,
  postApiProgramByIdUsersByUserId,
  getApiProgramByIdUsers,
  getApiProgramByIdUsersByUserIdProgress,
  getApiProgramsByProgramIdContent,
  postApiProgramsByProgramIdContent,
  getApiProgramsByProgramIdContentTopLevel,
  deleteApiProgramsByProgramIdContentById,
  getApiProgramsByProgramIdContentById,
  putApiProgramsByProgramIdContentById,
  getApiProgramsByProgramIdContentByParentIdChildren,
  postApiProgramsByProgramIdContentReorder,
  postApiProgramsByProgramIdContentByIdMove,
  getApiProgramsByProgramIdContentRequired,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  GetApiProgramData,
  PostApiProgramData,
  DeleteApiProgramByIdData,
  GetApiProgramByIdData,
  PutApiProgramByIdData,
  GetApiProgramPublishedData,
  GetApiProgramCategoryByCategoryData,
  GetApiProgramDifficultyByDifficultyData,
  GetApiProgramCreatorByCreatorIdData,
  GetApiProgramPopularData,
  GetApiProgramRecentData,
  GetApiProgramSearchData,
  GetApiProgramSlugBySlugData,
  GetApiProgramByIdWithContentData,
  PostApiProgramByIdCloneData,
  PostApiProgramByIdContentData,
  DeleteApiProgramByIdContentByContentIdData,
  PutApiProgramByIdContentByContentIdData,
  PostApiProgramByIdContentReorderData,
  DeleteApiProgramByIdUsersByUserIdData,
  PostApiProgramByIdUsersByUserIdData,
  GetApiProgramByIdUsersData,
  GetApiProgramByIdUsersByUserIdProgressData,
  GetApiProgramsByProgramIdContentData,
  PostApiProgramsByProgramIdContentData,
  GetApiProgramsByProgramIdContentTopLevelData,
  DeleteApiProgramsByProgramIdContentByIdData,
  GetApiProgramsByProgramIdContentByIdData,
  PutApiProgramsByProgramIdContentByIdData,
  GetApiProgramsByProgramIdContentByParentIdChildrenData,
  PostApiProgramsByProgramIdContentReorderData,
  PostApiProgramsByProgramIdContentByIdMoveData,
  GetApiProgramsByProgramIdContentRequiredData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// PROGRAM MANAGEMENT
// =============================================================================

/**
 * Get all programs with optional filtering
 */
export async function getPrograms(data?: GetApiProgramData) {
  await configureAuthenticatedClient();
  
  return getApiProgram({
    query: data?.query,
  });
}

/**
 * Create a new program
 */
export async function createProgram(data?: PostApiProgramData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgram({
    body: data?.body,
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  
  return result;
}

/**
 * Delete a program by ID
 */
export async function deleteProgram(data: DeleteApiProgramByIdData) {
  await configureAuthenticatedClient();
  
  const result = await deleteApiProgramById({
    path: data.path,
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  
  return result;
}

/**
 * Get a specific program by ID
 */
export async function getProgramById(data: GetApiProgramByIdData) {
  await configureAuthenticatedClient();
  
  return getApiProgramById({
    path: data.path,
  });
}

/**
 * Update a program by ID
 */
export async function updateProgram(data: PutApiProgramByIdData) {
  await configureAuthenticatedClient();
  
  const result = await putApiProgramById({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  
  return result;
}

/**
 * Get a program with its content
 */
export async function getProgramWithContent(data: GetApiProgramByIdWithContentData) {
  await configureAuthenticatedClient();
  
  return getApiProgramByIdWithContent({
    path: data.path,
  });
}

/**
 * Clone a program
 */
export async function cloneProgram(data: PostApiProgramByIdCloneData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdClone({
    path: data.path,
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  
  return result;
}

// =============================================================================
// PROGRAM DISCOVERY & SEARCH
// =============================================================================

/**
 * Get published programs
 */
export async function getPublishedPrograms(data?: GetApiProgramPublishedData) {
  await configureAuthenticatedClient();
  
  return getApiProgramPublished({
    query: data?.query,
  });
}

/**
 * Search programs
 */
export async function searchPrograms(data?: GetApiProgramSearchData) {
  await configureAuthenticatedClient();
  
  return getApiProgramSearch({
    query: data?.query,
  });
}

/**
 * Get program by slug
 */
export async function getProgramBySlug(data: GetApiProgramSlugBySlugData) {
  await configureAuthenticatedClient();
  
  return getApiProgramSlugBySlug({
    path: data.path,
  });
}

/**
 * Get programs by category
 */
export async function getProgramsByCategory(data: GetApiProgramCategoryByCategoryData) {
  await configureAuthenticatedClient();
  
  return getApiProgramCategoryByCategory({
    path: data.path,
  });
}

/**
 * Get programs by difficulty
 */
export async function getProgramsByDifficulty(data: GetApiProgramDifficultyByDifficultyData) {
  await configureAuthenticatedClient();
  
  return getApiProgramDifficultyByDifficulty({
    path: data.path,
  });
}

/**
 * Get programs by creator
 */
export async function getProgramsByCreator(data: GetApiProgramCreatorByCreatorIdData) {
  await configureAuthenticatedClient();
  
  return getApiProgramCreatorByCreatorId({
    path: data.path,
  });
}

/**
 * Get popular programs
 */
export async function getPopularPrograms(data?: GetApiProgramPopularData) {
  await configureAuthenticatedClient();
  
  return getApiProgramPopular({
    query: data?.query,
  });
}

/**
 * Get recent programs
 */
export async function getRecentPrograms(data?: GetApiProgramRecentData) {
  await configureAuthenticatedClient();
  
  return getApiProgramRecent({
    query: data?.query,
  });
}

// =============================================================================
// PROGRAM CONTENT MANAGEMENT
// =============================================================================

/**
 * Create program content
 */
export async function createProgramContent(data: PostApiProgramByIdContentData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdContent({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate program content cache
  revalidateTag('program-content');
  
  return result;
}

/**
 * Delete program content
 */
export async function deleteProgramContent(data: DeleteApiProgramByIdContentByContentIdData) {
  await configureAuthenticatedClient();
  
  const result = await deleteApiProgramByIdContentByContentId({
    path: data.path,
  });
  
  // Revalidate program content cache
  revalidateTag('program-content');
  
  return result;
}

/**
 * Update program content
 */
export async function updateProgramContent(data: PutApiProgramByIdContentByContentIdData) {
  await configureAuthenticatedClient();
  
  const result = await putApiProgramByIdContentByContentId({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate program content cache
  revalidateTag('program-content');
  
  return result;
}

/**
 * Reorder program content
 */
export async function reorderProgramContent(data: PostApiProgramByIdContentReorderData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdContentReorder({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate program content cache
  revalidateTag('program-content');
  
  return result;
}

/**
 * Get program content (alternative API)
 */
export async function getProgramContent(data: GetApiProgramsByProgramIdContentData) {
  await configureAuthenticatedClient();
  
  return getApiProgramsByProgramIdContent({
    path: data.path,
  });
}

/**
 * Get top-level program content
 */
export async function getTopLevelProgramContent(data: GetApiProgramsByProgramIdContentTopLevelData) {
  await configureAuthenticatedClient();
  
  return getApiProgramsByProgramIdContentTopLevel({
    path: data.path,
  });
}

/**
 * Get program content by ID
 */
export async function getProgramContentById(data: GetApiProgramsByProgramIdContentByIdData) {
  await configureAuthenticatedClient();
  
  return getApiProgramsByProgramIdContentById({
    path: data.path,
  });
}

/**
 * Get program content children
 */
export async function getProgramContentChildren(data: GetApiProgramsByProgramIdContentByParentIdChildrenData) {
  await configureAuthenticatedClient();
  
  return getApiProgramsByProgramIdContentByParentIdChildren({
    path: data.path,
  });
}

/**
 * Get required program content
 */
export async function getRequiredProgramContent(data: GetApiProgramsByProgramIdContentRequiredData) {
  await configureAuthenticatedClient();
  
  return getApiProgramsByProgramIdContentRequired({
    path: data.path,
  });
}

// =============================================================================
// PROGRAM ENROLLMENT & USER MANAGEMENT
// =============================================================================

/**
 * Enroll user in program
 */
export async function enrollUserInProgram(data: PostApiProgramByIdUsersByUserIdData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdUsersByUserId({
    path: data.path,
  });
  
  // Revalidate program enrollments cache
  revalidateTag('program-enrollments');
  
  return result;
}

/**
 * Remove user from program
 */
export async function removeUserFromProgram(data: DeleteApiProgramByIdUsersByUserIdData) {
  await configureAuthenticatedClient();
  
  const result = await deleteApiProgramByIdUsersByUserId({
    path: data.path,
  });
  
  // Revalidate program enrollments cache
  revalidateTag('program-enrollments');
  
  return result;
}

/**
 * Get program users
 */
export async function getProgramUsers(data: GetApiProgramByIdUsersData) {
  await configureAuthenticatedClient();
  
  return getApiProgramByIdUsers({
    path: data.path,
  });
}

/**
 * Get user progress in program
 */
export async function getUserProgramProgress(data: GetApiProgramByIdUsersByUserIdProgressData) {
  await configureAuthenticatedClient();
  
  return getApiProgramByIdUsersByUserIdProgress({
    path: data.path,
  });
}
