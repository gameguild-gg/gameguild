'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteApiProgramById,
  deleteApiProgramByIdContentByContentId,
  deleteApiProgramByIdLinkProductByProductId,
  deleteApiProgramByIdUsersByUserId,
  getApiProgram,
  getApiProgramById,
  getApiProgramByIdAnalytics,
  getApiProgramByIdAnalyticsCompletionRates,
  getApiProgramByIdAnalyticsEngagement,
  getApiProgramByIdAnalyticsRevenue,
  getApiProgramByIdPricing,
  getApiProgramByIdProducts,
  getApiProgramByIdUsers,
  getApiProgramByIdUsersByUserIdProgress,
  getApiProgramByIdWithContent,
  getApiProgramCategoryByCategory,
  getApiProgramCreatorByCreatorId,
  getApiProgramDifficultyByDifficulty,
  getApiProgramPopular,
  getApiProgramPublished,
  getApiProgramRecent,
  getApiProgramsByProgramIdContent,
  getApiProgramsByProgramIdContentById,
  getApiProgramsByProgramIdContentByParentIdChildren,
  getApiProgramsByProgramIdContentByTypeByType,
  getApiProgramsByProgramIdContentByVisibilityByVisibility,
  getApiProgramsByProgramIdContentRequired,
  getApiProgramsByProgramIdContentStats,
  getApiProgramsByProgramIdContentTopLevel,
  getApiProgramSearch,
  getApiProgramSlugBySlug,
  postApiProgram,
  postApiProgramByIdApprove,
  postApiProgramByIdArchive,
  postApiProgramByIdClone,
  postApiProgramByIdContent,
  postApiProgramByIdContentReorder,
  postApiProgramByIdCreateProduct,
  postApiProgramByIdDisableMonetization,
  postApiProgramByIdLinkProductByProductId,
  postApiProgramByIdMonetize,
  postApiProgramByIdPublish,
  postApiProgramByIdReject,
  postApiProgramByIdRestore,
  postApiProgramByIdSchedule,
  postApiProgramByIdSubmit,
  postApiProgramByIdUnpublish,
  postApiProgramByIdUsersByUserId,
  postApiProgramByIdUsersByUserIdContentByContentIdComplete,
  postApiProgramByIdUsersByUserIdReset,
  postApiProgramByIdWithdraw,
  postApiProgramsByProgramIdContentSearch,
  putApiProgramById,
  putApiProgramByIdContentByContentId,
  putApiProgramByIdPricing,
  putApiProgramByIdUsersByUserIdProgress,
} from '@/lib/api/generated/sdk.gen';
import type {
  DeleteApiProgramByIdContentByContentIdData,
  DeleteApiProgramByIdData,
  DeleteApiProgramByIdUsersByUserIdData,
  GetApiProgramByIdData,
  GetApiProgramByIdUsersByUserIdProgressData,
  GetApiProgramByIdUsersData,
  GetApiProgramByIdWithContentData,
  GetApiProgramCategoryByCategoryData,
  GetApiProgramCreatorByCreatorIdData,
  GetApiProgramData,
  GetApiProgramDifficultyByDifficultyData,
  GetApiProgramPopularData,
  GetApiProgramPublishedData,
  GetApiProgramRecentData,
  GetApiProgramsByProgramIdContentByIdData,
  GetApiProgramsByProgramIdContentByParentIdChildrenData,
  GetApiProgramsByProgramIdContentData,
  GetApiProgramsByProgramIdContentRequiredData,
  GetApiProgramsByProgramIdContentTopLevelData,
  GetApiProgramSearchData,
  GetApiProgramSlugBySlugData,
  PostApiProgramByIdCloneData,
  PostApiProgramByIdContentData,
  PostApiProgramByIdContentReorderData,
  PostApiProgramByIdUsersByUserIdData,
  PostApiProgramData,
  ProgramContentType,
  PutApiProgramByIdContentByContentIdData,
  PutApiProgramByIdData,
  SearchContentDto,
  Visibility,
} from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// PROGRAM MANAGEMENT
// =============================================================================

/**
 * Get all programs with optional filtering
 */
export async function getPrograms(data?: GetApiProgramData) {
  try {
    await configureAuthenticatedClient();

    return getApiProgram({
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
export async function createProgram(data?: Omit<PostApiProgramData, 'url'>) {
  try {
    console.log('createProgram called with data:', data?.body);

    await configureAuthenticatedClient();
    console.log('Client configured successfully');

    console.log('Creating program with data:', data?.body);

    const result = await postApiProgram({
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
export async function getProgramById(data: Omit<GetApiProgramByIdData, 'url'>) {
  await configureAuthenticatedClient();

  return getApiProgramById({
    path: data.path,
  });
}

/**
 * Update a program by ID
 */
export async function updateProgram(data: Omit<PutApiProgramByIdData, 'url'>) {
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
export async function getProgramWithContent(data: Omit<GetApiProgramByIdWithContentData, 'url'>) {
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
export async function getProgramBySlug(data: Omit<GetApiProgramSlugBySlugData, 'url'>) {
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
export async function createProgramContent(data: Omit<PostApiProgramByIdContentData, 'url'>) {
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
export async function deleteProgramContent(data: Omit<DeleteApiProgramByIdContentByContentIdData, 'url'>) {
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
export async function reorderProgramContent(data: Omit<PostApiProgramByIdContentReorderData, 'url'>) {
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
export async function getProgramContentByType(programId: string, type: ProgramContentType) {
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
export async function searchContentInProgram(programId: string, searchTerm: string, searchData?: Partial<SearchContentDto>) {
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
