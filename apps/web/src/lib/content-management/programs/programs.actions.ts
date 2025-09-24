'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteProgramById,
  deleteProgramByIdContentByContentId,
  deleteProgramByIdLinkProductByProductId,
  deleteProgramByIdUsersByUserId,
  getApiProgramsByProgramIdContent,
  getApiProgramsByProgramIdContentById,
  getApiProgramsByProgramIdContentByParentIdChildren,
  getApiProgramsByProgramIdContentByTypeByType,
  getApiProgramsByProgramIdContentByVisibilityByVisibility,
  getApiProgramsByProgramIdContentRequired,
  getApiProgramsByProgramIdContentStats,
  getApiProgramsByProgramIdContentTopLevel,
  getProgram,
  getProgramByIdAnalytics,
  getProgramByIdAnalyticsCompletionRates,
  getProgramByIdAnalyticsEngagement,
  getProgramByIdAnalyticsRevenue,
  getProgramById as getProgramByIdFromApi,
  getProgramByIdPricing,
  getProgramByIdProducts,
  getProgramByIdUsers,
  getProgramByIdUsersByUserIdProgress,
  getProgramByIdWithContent,
  getProgramCategoryByCategory,
  getProgramCreatorByCreatorId,
  getProgramDifficultyByDifficulty,
  getProgramPopular,
  getProgramPublished,
  getProgramRecent,
  getProgramSearch,
  getProgramSlugBySlug,
  postApiProgramsByProgramIdContentSearch,
  postProgram,
  postProgramByIdApprove,
  postProgramByIdArchive,
  postProgramByIdClone,
  postProgramByIdContent,
  postProgramByIdContentReorder,
  postProgramByIdCreateProduct,
  postProgramByIdDisableMonetization,
  postProgramByIdLinkProductByProductId,
  postProgramByIdMonetize,
  postProgramByIdPublish,
  postProgramByIdReject,
  postProgramByIdRestore,
  postProgramByIdSchedule,
  postProgramByIdSubmit,
  postProgramByIdUnpublish,
  postProgramByIdUsersByUserId,
  postProgramByIdUsersByUserIdContentByContentIdComplete,
  postProgramByIdUsersByUserIdReset,
  postProgramByIdWithdraw,
  putProgramById,
  putProgramByIdContentByContentId,
  putProgramByIdPricing,
  putProgramByIdUsersByUserIdProgress,
} from '@/lib/api/generated/sdk.gen';
import type {
  DeleteProgramByIdContentByContentIdData,
  DeleteProgramByIdData,
  DeleteProgramByIdUsersByUserIdData,
  GetApiProgramsByProgramIdContentByIdData,
  GetApiProgramsByProgramIdContentByParentIdChildrenData,
  GetApiProgramsByProgramIdContentData,
  GetApiProgramsByProgramIdContentRequiredData,
  GetApiProgramsByProgramIdContentTopLevelData,
  GetProgramByIdData,
  GetProgramByIdUsersByUserIdProgressData,
  GetProgramByIdUsersData,
  GetProgramByIdWithContentData,
  GetProgramCategoryByCategoryData,
  GetProgramCreatorByCreatorIdData,
  GetProgramData,
  GetProgramDifficultyByDifficultyData,
  GetProgramPopularData,
  GetProgramPublishedData,
  GetProgramRecentData,
  GetProgramSearchData,
  GetProgramSlugBySlugData,
  ModulesProgramsSearchContentDto,
  PostProgramByIdCloneData,
  PostProgramByIdContentData,
  PostProgramByIdContentReorderData,
  PostProgramByIdUsersByUserIdData,
  PostProgramData,
  PutProgramByIdContentByContentIdData,
  PutProgramByIdData,
  SourceModulesProgramsModelsProgramContentType,
  Visibility,
} from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// PROGRAM MANAGEMENT
// =============================================================================

/**
 * Get all programs with optional filtering
 */
export async function getPrograms(data?: GetProgramData) {
  try {
    await configureAuthenticatedClient();

    return getProgram({
      query: data?.query,
    });
  } catch (error) {
    console.error('Error in getPrograms:', error);

    if (error instanceof Error) {
      if (error.message.includes('Authentication required') || error.message.includes('no access token')) {
        throw new Error('Please sign in to access courses');
      }
      if (error.message.includes('fetch failed') || error.message.includes('ECONNREFUSED')) {
        throw new Error('Unable to connect to server. Please check if the API is running.');
      }
      throw error;
    }

    throw new Error('Failed to load courses');
  }
}

/**
 * Create a new program
 */
export async function createProgram(data?: Omit<PostProgramData, 'url'>) {
  try {
    console.log('createProgram called with data:', data?.body);

    await configureAuthenticatedClient();
    console.log('Client configured successfully');

    console.log('Creating program with data:', data?.body);

    const result = await postProgram({
      body: data?.body,
    });

    console.log('Raw API response:', result);
    console.log('API response result:', {
      hasData: !!result.data,
      hasError: !!result.error,
      dataKeys: result.data ? Object.keys(result.data) : null,
      errorType: result.error ? typeof result.error : null,
      error: result.error
    });

    // Check for errors in the response
    if (result.error || (result.response && !result.response.ok)) {
      let errorMessage = 'An unexpected error occurred';
      let errorStatus = result.response?.status || 500;
      let errorType = 'unknown_error';

      // Extract detailed error message from the API response
      if (result.error) {
        if (typeof result.error === 'string') {
          errorMessage = result.error;
        } else if (typeof result.error === 'object' && result.error !== null) {
          // Handle ProblemDetails format from .NET API
          if ('detail' in result.error && typeof result.error.detail === 'string') {
            errorMessage = result.error.detail;
          } else if ('message' in result.error && typeof result.error.message === 'string') {
            errorMessage = result.error.message;
          } else if ('title' in result.error && typeof result.error.title === 'string') {
            errorMessage = result.error.title;
          } else {
            // Fallback: stringify the error object
            errorMessage = JSON.stringify(result.error);
          }
        }
      }

      // Determine error type based on status code
      if (errorStatus === 403) {
        errorType = 'permission_denied';
        console.error('Permission denied:', errorMessage);
      } else if (errorStatus === 401) {
        errorType = 'authentication_required';
      } else if (errorStatus === 409) {
        errorType = 'conflict';
      } else if (errorStatus === 400) {
        errorType = 'validation_error';
      }

      return {
        data: null,
        error: {
          message: errorMessage,
          status: errorStatus,
          type: errorType
        }
      };
    }

    // Revalidate programs cache
    revalidateTag('programs');

    // Return only the serializable data, not the Response object
    return {
      data: result.data,
      error: result.error || null
    };
  } catch (error) {
    console.error('Error in createProgram:', error);

    // Log detailed error information
    if (error instanceof Error) {
      console.error('Error details:', {
        message: error.message,
        name: error.name,
        stack: error.stack,
        cause: error.cause
      });
    }

    // Return a serializable error object instead of throwing
    return {
      data: null,
      error: {
        message: error instanceof Error ? error.message : 'An unexpected error occurred',
        status: 'error'
      }
    };
  }
}

/**
 * Delete a program by ID
 */
export async function deleteProgram(data: DeleteProgramByIdData) {
  await configureAuthenticatedClient();

  const result = await deleteProgramById({
    path: data.path,
  });

  // Revalidate programs cache
  revalidateTag('programs');

  return result;
}

/**
 * Get a specific program by ID
 */
export async function getProgramById(data: Omit<GetProgramByIdData, 'url'>) {
  await configureAuthenticatedClient();

  return getProgramByIdFromApi({
    path: data.path,
  });
}

/**
 * Update a program by ID
 */
export async function updateProgram(data: Omit<PutProgramByIdData, 'url'>) {
  await configureAuthenticatedClient();

  const result = await putProgramById({
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
export async function getProgramWithContent(data: Omit<GetProgramByIdWithContentData, 'url'>) {
  await configureAuthenticatedClient();

  return getProgramByIdWithContent({
    path: data.path,
  });
}

/**
 * Clone a program
 */
export async function cloneProgram(data: PostProgramByIdCloneData) {
  await configureAuthenticatedClient();

  const result = await postProgramByIdClone({
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
export async function getPublishedPrograms(data?: GetProgramPublishedData) {
  await configureAuthenticatedClient();

  return getProgramPublished({
    query: data?.query,
  });
}

/**
 * Search programs
 */
export async function searchPrograms(data?: GetProgramSearchData) {
  await configureAuthenticatedClient();

  return getProgramSearch({
    query: data?.query,
  });
}

/**
 * Get program by slug
 */
export async function getProgramBySlug(data: Omit<GetProgramSlugBySlugData, 'url'>) {
  await configureAuthenticatedClient();

  return getProgramSlugBySlug({
    path: data.path,
  });
}

/**
 * Get programs by category
 */
export async function getProgramsByCategory(data: GetProgramCategoryByCategoryData) {
  await configureAuthenticatedClient();

  return getProgramCategoryByCategory({
    path: data.path,
  });
}

/**
 * Get programs by difficulty
 */
export async function getProgramsByDifficulty(data: GetProgramDifficultyByDifficultyData) {
  await configureAuthenticatedClient();

  return getProgramDifficultyByDifficulty({
    path: data.path,
  });
}

/**
 * Get programs by creator
 */
export async function getProgramsByCreator(data: GetProgramCreatorByCreatorIdData) {
  await configureAuthenticatedClient();

  return getProgramCreatorByCreatorId({
    path: data.path,
  });
}

/**
 * Get popular programs
 */
export async function getPopularPrograms(data?: GetProgramPopularData) {
  await configureAuthenticatedClient();

  return getProgramPopular({
    query: data?.query,
  });
}

/**
 * Get recent programs
 */
export async function getRecentPrograms(data?: GetProgramRecentData) {
  await configureAuthenticatedClient();

  return getProgramRecent({
    query: data?.query,
  });
}

// =============================================================================
// PROGRAM CONTENT MANAGEMENT
// =============================================================================

/**
 * Create program content
 */
export async function createProgramContent(data: Omit<PostProgramByIdContentData, 'url'>) {
  await configureAuthenticatedClient();

  const result = await postProgramByIdContent({
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
export async function deleteProgramContent(data: Omit<DeleteProgramByIdContentByContentIdData, 'url'>) {
  await configureAuthenticatedClient();

  const result = await deleteProgramByIdContentByContentId({
    path: data.path,
  });

  // Revalidate program content cache
  revalidateTag('program-content');

  return result;
}

/**
 * Update program content
 */
export async function updateProgramContent(data: PutProgramByIdContentByContentIdData) {
  await configureAuthenticatedClient();

  const result = await putProgramByIdContentByContentId({
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
export async function reorderProgramContent(data: Omit<PostProgramByIdContentReorderData, 'url'>) {
  await configureAuthenticatedClient();

  const result = await postProgramByIdContentReorder({
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
export async function getTopLevelProgramContent(data: Omit<GetApiProgramsByProgramIdContentTopLevelData, 'url'>) {
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
export async function getProgramContentChildren(data: Omit<GetApiProgramsByProgramIdContentByParentIdChildrenData, 'url'>) {
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
export async function enrollUserInProgram(data: PostProgramByIdUsersByUserIdData) {
  await configureAuthenticatedClient();

  const result = await postProgramByIdUsersByUserId({
    path: data.path,
  });

  // Revalidate program enrollments cache
  revalidateTag('program-enrollments');

  return result;
}

/**
 * Remove user from program
 */
export async function removeUserFromProgram(data: DeleteProgramByIdUsersByUserIdData) {
  await configureAuthenticatedClient();

  const result = await deleteProgramByIdUsersByUserId({
    path: data.path,
  });

  // Revalidate program enrollments cache
  revalidateTag('program-enrollments');

  return result;
}

/**
 * Get program users
 */
export async function getProgramUsers(data: GetProgramByIdUsersData) {
  await configureAuthenticatedClient();

  return getProgramByIdUsers({
    path: data.path,
  });
}

/**
 * Get user progress in program
 */
export async function getUserProgramProgress(data: GetProgramByIdUsersByUserIdProgressData) {
  await configureAuthenticatedClient();

  return getProgramByIdUsersByUserIdProgress({
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

  const response = await postProgramByIdApprove({
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

  const response = await postProgramByIdReject({
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

  const response = await postProgramByIdArchive({
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

  const response = await postProgramByIdRestore({
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

  const response = await postProgramByIdPublish({
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

  const response = await postProgramByIdSchedule({
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

  const response = await postProgramByIdMonetize({
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

  const response = await postProgramByIdDisableMonetization({
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

  return getProgramByIdPricing({
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

  return getProgramByIdAnalytics({
    path: { id: programId },
  });
}

/**
 * Get program completion rates analytics
 */
export async function getProgramCompletionRates(programId: string) {
  await configureAuthenticatedClient();

  return getProgramByIdAnalyticsCompletionRates({
    path: { id: programId },
  });
}

/**
 * Get program engagement analytics
 */
export async function getProgramEngagement(programId: string) {
  await configureAuthenticatedClient();

  return getProgramByIdAnalyticsEngagement({
    path: { id: programId },
  });
}

/**
 * Get program revenue analytics
 */
export async function getProgramRevenue(programId: string) {
  await configureAuthenticatedClient();

  return getProgramByIdAnalyticsRevenue({
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

  return getProgramByIdProducts({
    path: { id: programId },
  });
}

/**
 * Create product for program
 */
export async function createProgramProduct(programId: string, productData: object) {
  await configureAuthenticatedClient();

  const response = await postProgramByIdCreateProduct({
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

  const response = await postProgramByIdLinkProductByProductId({
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

  const response = await deleteProgramByIdLinkProductByProductId({
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
export async function getProgramContentByType(programId: string, type: SourceModulesProgramsModelsProgramContentType) {
  await configureAuthenticatedClient();

  return getApiProgramsByProgramIdContentByTypeByType({
    path: { programId, type },
  });
}

/**
 * Get program content by visibility
 */
export async function getProgramContentByVisibility(programId: string, visibility: Visibility) {
  await configureAuthenticatedClient();

  return getApiProgramsByProgramIdContentByVisibilityByVisibility({
    path: { programId, visibility },
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
export async function searchContentInProgram(programId: string, searchTerm: string, searchData?: Partial<ModulesProgramsSearchContentDto>) {
  await configureAuthenticatedClient();

  const result = await postApiProgramsByProgramIdContentSearch({
    path: { programId },
    body: {
      programId,
      searchTerm,
      ...searchData,
    },
  });

  return result;
}

/**
 * Submit a program for review
 */
export async function submitProgram(programId: string) {
  await configureAuthenticatedClient();

  const result = await postProgramByIdSubmit({
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

  const result = await postProgramByIdUnpublish({
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

  const result = await postProgramByIdUsersByUserIdContentByContentIdComplete({
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

  const result = await postProgramByIdUsersByUserIdReset({
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

  const result = await postProgramByIdWithdraw({
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

  const result = await putProgramByIdPricing({
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

  const result = await putProgramByIdUsersByUserIdProgress({
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
