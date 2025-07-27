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
  // Program Management & Publishing Operations
  postApiProgramByIdApprove,
  postApiProgramByIdReject,
  postApiProgramByIdArchive,
  postApiProgramByIdRestore,
  postApiProgramByIdPublish,
  postApiProgramByIdSchedule,
  // Program Monetization Operations
  postApiProgramByIdMonetize,
  postApiProgramByIdDisableMonetization,
  getApiProgramByIdPricing,
  // Program Analytics Operations
  getApiProgramByIdAnalytics,
  getApiProgramByIdAnalyticsCompletionRates,
  getApiProgramByIdAnalyticsEngagement,
  getApiProgramByIdAnalyticsRevenue,
  // Program Product Operations
  getApiProgramByIdProducts,
  postApiProgramByIdCreateProduct,
  postApiProgramByIdLinkProductByProductId,
  deleteApiProgramByIdLinkProductByProductId,
  // Program Content Advanced Operations
  getApiProgramsByProgramIdContentByTypeByType,
  getApiProgramsByProgramIdContentByVisibilityByVisibility,
  getApiProgramsByProgramIdContentStats,
  // Additional Program Operations
  postApiProgramByIdSubmit,
  postApiProgramByIdUnpublish,
  postApiProgramByIdUsersByUserIdContentByContentIdComplete,
  postApiProgramByIdUsersByUserIdReset,
  postApiProgramByIdWithdraw,
  postApiProgramsByProgramIdContentSearch,
  putApiProgramByIdPricing,
  putApiProgramByIdUsersByUserIdProgress,
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
  // Program Management & Publishing Types
  // Program Monetization Types
  // Program Analytics Types
  // Program Product Types
  // Program Content Advanced Types
  // Additional Program Types
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

// =============================================================================
// PROGRAM MANAGEMENT & PUBLISHING OPERATIONS
// =============================================================================

/**
 * Approve program for publication
 */
export async function approveProgram(programId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdApprove({
    path: { id: programId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

/**
 * Reject program publication
 */
export async function rejectProgram(programId: string, reason?: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdReject({
    path: { id: programId },
    body: reason ? { reason } : undefined,
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

/**
 * Archive program
 */
export async function archiveProgram(programId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdArchive({
    path: { id: programId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

/**
 * Restore archived program
 */
export async function restoreProgram(programId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdRestore({
    path: { id: programId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

/**
 * Publish program
 */
export async function publishProgram(programId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdPublish({
    path: { id: programId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  revalidateTag('published-programs');
  return response;
}

/**
 * Schedule program publication
 */
export async function scheduleProgram(programId: string, publishAt: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdSchedule({
    path: { id: programId },
    body: { publishAt },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

// =============================================================================
// PROGRAM MONETIZATION OPERATIONS
// =============================================================================

/**
 * Enable monetization for program
 */
export async function monetizeProgram(programId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdMonetize({
    path: { id: programId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

/**
 * Disable monetization for program
 */
export async function disableProgramMonetization(programId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdDisableMonetization({
    path: { id: programId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  return response;
}

/**
 * Get program pricing information
 */
export async function getProgramPricing(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramByIdPricing({
    path: { id: programId },
  });
}

// =============================================================================
// PROGRAM ANALYTICS OPERATIONS  
// =============================================================================

/**
 * Get program analytics overview
 */
export async function getProgramAnalytics(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramByIdAnalytics({
    path: { id: programId },
  });
}

/**
 * Get program completion rates analytics
 */
export async function getProgramCompletionRates(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramByIdAnalyticsCompletionRates({
    path: { id: programId },
  });
}

/**
 * Get program engagement analytics
 */
export async function getProgramEngagement(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramByIdAnalyticsEngagement({
    path: { id: programId },
  });
}

/**
 * Get program revenue analytics
 */
export async function getProgramRevenue(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramByIdAnalyticsRevenue({
    path: { id: programId },
  });
}

// =============================================================================
// PROGRAM PRODUCT OPERATIONS
// =============================================================================

/**
 * Get products linked to program
 */
export async function getProgramProducts(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramByIdProducts({
    path: { id: programId },
  });
}

/**
 * Create product for program
 */
export async function createProgramProduct(programId: string, productData: object) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdCreateProduct({
    path: { id: programId },
    body: productData,
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  revalidateTag('products');
  return response;
}

/**
 * Link existing product to program
 */
export async function linkProductToProgram(programId: string, productId: string) {
  await configureAuthenticatedClient();

  const response = await postApiProgramByIdLinkProductByProductId({
    path: { id: programId, productId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  revalidateTag(`product-${productId}`);
  return response;
}

/**
 * Unlink product from program
 */
export async function unlinkProductFromProgram(programId: string, productId: string) {
  await configureAuthenticatedClient();

  const response = await deleteApiProgramByIdLinkProductByProductId({
    path: { id: programId, productId },
  });

  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  revalidateTag(`product-${productId}`);
  return response;
}

// =============================================================================
// PROGRAM CONTENT ADVANCED OPERATIONS
// =============================================================================

/**
 * Get program content by type
 */
export async function getProgramContentByType(programId: string, type: string) {
  await configureAuthenticatedClient();

  return getApiProgramsByProgramIdContentByTypeByType({
    path: { programId, type: type as 'lesson' | 'quiz' | 'assignment' | 'resource' },
  });
}

/**
 * Get program content by visibility
 */
export async function getProgramContentByVisibility(programId: string, visibility: string) {
  await configureAuthenticatedClient();

  return getApiProgramsByProgramIdContentByVisibilityByVisibility({
    path: { programId, visibility: visibility as 'public' | 'private' | 'unlisted' },
  });
}

/**
 * Get program content statistics
 */
export async function getProgramContentStats(programId: string) {
  await configureAuthenticatedClient();

  return getApiProgramsByProgramIdContentStats({
    path: { programId },
  });
}

/**
 * Search content in a program
 */
export async function searchContentInProgram(programId: string, searchData: object) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramsByProgramIdContentSearch({
    path: { programId },
    body: searchData,
  });
  
  return result;
}

/**
 * Submit a program for review
 */
export async function submitProgram(programId: string) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdSubmit({
    path: { id: programId },
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  
  return result;
}

/**
 * Unpublish a program
 */
export async function unpublishProgram(programId: string) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdUnpublish({
    path: { id: programId },
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  
  return result;
}

/**
 * Mark user content as complete
 */
export async function completeUserContent(programId: string, userId: string, contentId: string) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdUsersByUserIdContentByContentIdComplete({
    path: {
      id: programId,
      userId,
      contentId,
    },
  });
  
  // Revalidate user progress cache
  revalidateTag(`program-${programId}-user-${userId}`);
  revalidateTag(`user-progress-${userId}`);
  
  return result;
}

/**
 * Reset user progress in a program
 */
export async function resetUserProgress(programId: string, userId: string) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdUsersByUserIdReset({
    path: {
      id: programId,
      userId,
    },
  });
  
  // Revalidate user progress cache
  revalidateTag(`program-${programId}-user-${userId}`);
  revalidateTag(`user-progress-${userId}`);
  
  return result;
}

/**
 * Withdraw from a program
 */
export async function withdrawFromProgram(programId: string) {
  await configureAuthenticatedClient();
  
  const result = await postApiProgramByIdWithdraw({
    path: { id: programId },
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  
  return result;
}

/**
 * Update program pricing
 */
export async function updateProgramPricing(programId: string, pricingData: object) {
  await configureAuthenticatedClient();
  
  const result = await putApiProgramByIdPricing({
    path: { id: programId },
    body: pricingData,
  });
  
  // Revalidate programs cache
  revalidateTag('programs');
  revalidateTag(`program-${programId}`);
  
  return result;
}

/**
 * Update user progress in a program
 */
export async function updateUserProgress(programId: string, userId: string, progressData: object) {
  await configureAuthenticatedClient();
  
  const result = await putApiProgramByIdUsersByUserIdProgress({
    path: {
      id: programId,
      userId,
    },
    body: progressData,
  });
  
  // Revalidate user progress cache
  revalidateTag(`program-${programId}-user-${userId}`);
  revalidateTag(`user-progress-${userId}`);
  
  return result;
}
