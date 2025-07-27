'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getApiProgram,
  postApiProgram,
  getApiProgramById,
  putApiProgramById,
  deleteApiProgramById,
  getApiProgramPublished,
  getApiProgramCategoryByCategory,
  getApiProgramDifficultyByDifficulty,
  getApiProgramSearch,
  getApiProgramCreatorByCreatorId,
  getApiProgramPopular,
  getApiProgramRecent,
  getApiProgramByIdWithContent,
  postApiProgramByIdClone,
  getApiProgramSlugBySlug,
  postApiProgramByIdContent,
  deleteApiProgramByIdContentByContentId,
  getApiProgramsByProgramIdContent,
  getApiProgramsByProgramIdContentById,
  putApiProgramsByProgramIdContentById,
  getApiProgramsByProgramIdContentTopLevel,
  deleteApiProgramsByProgramIdContentById,
  getApiProgramsByProgramIdContentByParentIdChildren,
  postApiProgramsByProgramIdContentReorder,
  postApiProgramsByProgramIdContentByIdMove,
  getApiProgramsByProgramIdContentRequired,
  getApiProgramsByProgramIdContentByTypeByType,
  getApiProgramsByProgramIdContentByVisibilityByVisibility,
  postApiProgramsByProgramIdContentSearch,
  getApiProgramsByProgramIdContentStats,
  postApiProgramByIdUsersByUserId,
  getApiProgramByIdUsers,
  deleteApiProgramByIdUsersByUserId,
  postApiProgramByIdSubmit,
  postApiProgramByIdApprove,
  postApiProgramByIdReject,
  postApiProgramByIdWithdraw,
} from '@/lib/api/generated/sdk.gen';
import type {
  CreateProgramDto,
  CloneProgramDto,
  CreateContentDto,
  UpdateContentDto,
  ProgramCategory,
  ProgramDifficulty,
  ProgramContentType,
  Visibility,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// CORE PROGRAM MANAGEMENT
// =============================================================================

/**
 * Get all programs with optional filters
 */
export async function getPrograms(params?: { skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgram({
      query: {
        skip: params?.skip,
        take: params?.take,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching programs:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch programs');
  }
}

/**
 * Create a new program
 */
export async function createProgram(programData: CreateProgramDto) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgram({
      body: programData,
    });

    revalidateTag('programs');
    revalidateTag('programs-published');
    return response.data;
  } catch (error) {
    console.error('Error creating program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create program');
  }
}

/**
 * Get program by ID
 */
export async function getProgramById(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramById({
      path: { id: programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program by ID:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program');
  }
}

/**
 * Update program by ID
 */
export async function updateProgram(programId: string, programData: CreateProgramDto) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiProgramById({
      path: { id: programId },
      body: programData,
    });

    revalidateTag('programs');
    revalidateTag('programs-published');
    revalidateTag(`program-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update program');
  }
}

/**
 * Delete program by ID
 */
export async function deleteProgram(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProgramById({
      path: { id: programId },
    });

    revalidateTag('programs');
    revalidateTag('programs-published');
    revalidateTag(`program-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error deleting program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete program');
  }
}

/**
 * Get program by slug (SEO-friendly)
 */
export async function getProgramBySlug(slug: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramSlugBySlug({
      path: { slug },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program by slug:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program');
  }
}

/**
 * Get program with all content included
 */
export async function getProgramWithContent(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramByIdWithContent({
      path: { id: programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program with content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program with content');
  }
}

/**
 * Clone an existing program
 */
export async function cloneProgram(programId: string, cloneData?: CloneProgramDto) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdClone({
      path: { id: programId },
      body: cloneData,
    });

    revalidateTag('programs');
    return response.data;
  } catch (error) {
    console.error('Error cloning program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to clone program');
  }
}

// =============================================================================
// PROGRAM DISCOVERY & FILTERING
// =============================================================================

/**
 * Get published programs
 */
export async function getPublishedPrograms(params?: { skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramPublished({
      query: {
        skip: params?.skip,
        take: params?.take,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching published programs:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch published programs');
  }
}

/**
 * Get programs by category
 */
export async function getProgramsByCategory(category: ProgramCategory, params?: { skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramCategoryByCategory({
      path: { category },
      query: {
        skip: params?.skip,
        take: params?.take,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching programs by category:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch programs by category');
  }
}

/**
 * Get programs by difficulty level
 */
export async function getProgramsByDifficulty(difficulty: ProgramDifficulty, params?: { skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramDifficultyByDifficulty({
      path: { difficulty },
      query: {
        skip: params?.skip,
        take: params?.take,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching programs by difficulty:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch programs by difficulty');
  }
}

/**
 * Search programs
 */
export async function searchPrograms(searchTerm: string, params?: { skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramSearch({
      query: {
        searchTerm,
        skip: params?.skip,
        take: params?.take,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error searching programs:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search programs');
  }
}

/**
 * Get programs by creator
 */
export async function getProgramsByCreator(creatorId: string, params?: { skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramCreatorByCreatorId({
      path: { creatorId },
      query: {
        skip: params?.skip,
        take: params?.take,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching programs by creator:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch programs by creator');
  }
}

/**
 * Get popular programs
 */
export async function getPopularPrograms(params?: { count?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramPopular({
      query: {
        count: params?.count,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching popular programs:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch popular programs');
  }
}

/**
 * Get recent programs
 */
export async function getRecentPrograms(params?: { count?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramRecent({
      query: {
        count: params?.count,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching recent programs:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch recent programs');
  }
}

// =============================================================================
// PROGRAM CONTENT MANAGEMENT
// =============================================================================

/**
 * Get all content for a program
 */
export async function getProgramContent(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContent({
      path: { programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program content');
  }
}

/**
 * Add content to a program
 */
export async function addContentToProgram(programId: string, contentData: CreateContentDto) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdContent({
      path: { id: programId },
      body: contentData,
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-content-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error adding content to program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to add content to program');
  }
}

/**
 * Get specific content by ID
 */
export async function getProgramContentById(programId: string, contentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentById({
      path: { programId, id: contentId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program content by ID:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program content');
  }
}

/**
 * Update program content
 */
export async function updateProgramContent(programId: string, contentId: string, contentData: UpdateContentDto) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiProgramsByProgramIdContentById({
      path: { programId, id: contentId },
      body: { ...contentData, id: contentId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-content-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating program content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update program content');
  }
}

/**
 * Remove content from program
 */
export async function removeProgramContent(programId: string, contentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProgramsByProgramIdContentById({
      path: { programId, id: contentId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-content-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error removing program content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove program content');
  }
}

// =============================================================================
// PROGRAM USER MANAGEMENT
// =============================================================================

/**
 * Get users enrolled in a program
 */
export async function getProgramUsers(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramByIdUsers({
      path: { id: programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program users:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program users');
  }
}

/**
 * Enroll user in program
 */
export async function enrollUserInProgram(programId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdUsersByUserId({
      path: { id: programId, userId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-users-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error enrolling user in program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to enroll user in program');
  }
}

/**
 * Remove user from program
 */
export async function removeUserFromProgram(programId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProgramByIdUsersByUserId({
      path: { id: programId, userId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-users-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error removing user from program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove user from program');
  }
}

// =============================================================================
// PROGRAM APPROVAL WORKFLOW
// =============================================================================

/**
 * Submit program for approval
 */
export async function submitProgramForApproval(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdSubmit({
      path: { id: programId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag('programs');
    return response.data;
  } catch (error) {
    console.error('Error submitting program for approval:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit program for approval');
  }
}

/**
 * Approve a program
 */
export async function approveProgram(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdApprove({
      path: { id: programId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag('programs');
    revalidateTag('programs-published');
    return response.data;
  } catch (error) {
    console.error('Error approving program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to approve program');
  }
}

/**
 * Reject a program
 */
export async function rejectProgram(programId: string, rejectionReason?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdReject({
      path: { id: programId },
      body: rejectionReason ? { reason: rejectionReason } : undefined,
    });

    revalidateTag(`program-${programId}`);
    revalidateTag('programs');
    return response.data;
  } catch (error) {
    console.error('Error rejecting program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to reject program');
  }
}

/**
 * Withdraw a program from approval
 */
export async function withdrawProgram(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramByIdWithdraw({
      path: { id: programId },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag('programs');
    return response.data;
  } catch (error) {
    console.error('Error withdrawing program:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to withdraw program');
  }
}

// =============================================================================
// ADVANCED CONTENT MANAGEMENT
// =============================================================================

/**
 * Get top-level content for a program
 */
export async function getProgramTopLevelContent(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentTopLevel({
      path: { programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program top-level content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program top-level content');
  }
}

/**
 * Get children content by parent ID
 */
export async function getProgramContentChildren(programId: string, parentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentByParentIdChildren({
      path: { programId, parentId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program content children:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program content children');
  }
}

/**
 * Reorder program content
 */
export async function reorderProgramContent(
  programId: string,
  reorderData: {
    contentIds: string[];
    newOrder: number[];
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramsByProgramIdContentReorder({
      path: { programId },
      body: reorderData,
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-content-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error reordering program content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to reorder program content');
  }
}

/**
 * Move content to different parent/position
 */
export async function moveProgramContent(
  programId: string,
  contentId: string,
  moveData: {
    newParentId?: string;
    newSortOrder: number;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramsByProgramIdContentByIdMove({
      path: { programId, id: contentId },
      body: {
        contentId,
        newParentId: moveData.newParentId,
        newSortOrder: moveData.newSortOrder,
      },
    });

    revalidateTag(`program-${programId}`);
    revalidateTag(`program-content-${programId}`);
    return response.data;
  } catch (error) {
    console.error('Error moving program content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to move program content');
  }
}

/**
 * Get required content for a program
 */
export async function getProgramRequiredContent(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentRequired({
      path: { programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program required content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program required content');
  }
}

/**
 * Get content by type
 */
export async function getProgramContentByType(programId: string, contentType: ProgramContentType) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentByTypeByType({
      path: { programId, type: contentType },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program content by type:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program content by type');
  }
}

/**
 * Get content by visibility
 */
export async function getProgramContentByVisibility(programId: string, visibility: Visibility) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentByVisibilityByVisibility({
      path: { programId, visibility },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program content by visibility:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program content by visibility');
  }
}

/**
 * Search program content
 */
export async function searchProgramContent(
  programId: string,
  searchData: {
    searchTerm: string;
    type?: ProgramContentType;
    visibility?: Visibility;
    isRequired?: boolean;
    parentId?: string;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramsByProgramIdContentSearch({
      path: { programId },
      body: {
        programId,
        searchTerm: searchData.searchTerm,
        type: searchData.type,
        visibility: searchData.visibility,
        isRequired: searchData.isRequired,
        parentId: searchData.parentId,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error searching program content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search program content');
  }
}

/**
 * Get program content statistics
 */
export async function getProgramContentStats(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdContentStats({
      path: { programId },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching program content stats:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch program content stats');
  }
}
