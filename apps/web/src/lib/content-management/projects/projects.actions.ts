'use server';

import { revalidateTag } from 'next/cache';
import {
  deleteApiProjectsById,
  getApiProjects,
  getApiProjectsById,
  getApiProjectsFeatured,
  getApiProjectsPopular,
  getApiProjectsRecent,
  getApiProjectsSearch,
  getApiProjectsSlugBySlug,
  getApiProjectsByIdStatistics,
  getApiProjectsCategoryByCategoryId,
  getApiProjectsCreatorByCreatorId,
  postApiProjects,
  postApiProjectsByIdArchive,
  postApiProjectsByIdPublish,
  postApiProjectsByIdUnpublish,
  putApiProjectsById,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  PostApiProjectsData,
  PutApiProjectsByIdData,
  GetApiProjectsData,
  DeleteApiProjectsByIdData,
  GetApiProjectsByIdData,
  GetApiProjectsSlugBySlugData,
  PostApiProjectsByIdPublishData,
  PostApiProjectsByIdUnpublishData,
  PostApiProjectsByIdArchiveData,
  GetApiProjectsSearchData,
  GetApiProjectsPopularData,
  GetApiProjectsRecentData,
  GetApiProjectsFeaturedData,
  GetApiProjectsByIdStatisticsData,
  GetApiProjectsCategoryByCategoryIdData,
  GetApiProjectsCreatorByCreatorIdData,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// PROJECT CRUD OPERATIONS
// =============================================================================

/**
 * Get all projects with optional filtering
 */
export async function getProjects(data?: GetApiProjectsData) {
  await configureAuthenticatedClient();
  
  return getApiProjects({
    query: data?.query,
  });
}

/**
 * Create a new project
 */
export async function createProject(data?: PostApiProjectsData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProjects({
    body: data?.body,
  });
  
  // Revalidate projects cache
  revalidateTag('projects');
  
  return result;
}

/**
 * Delete a project by ID
 */
export async function deleteProject(data: DeleteApiProjectsByIdData) {
  await configureAuthenticatedClient();
  
  const result = await deleteApiProjectsById({
    path: data.path,
  });
  
  // Revalidate projects cache
  revalidateTag('projects');
  
  return result;
}

/**
 * Get a specific project by ID
 */
export async function getProjectById(data: GetApiProjectsByIdData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsById({
    path: data.path,
  });
}

/**
 * Update a project by ID
 */
export async function updateProject(data: PutApiProjectsByIdData) {
  await configureAuthenticatedClient();
  
  const result = await putApiProjectsById({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate projects cache
  revalidateTag('projects');
  
  return result;
}

/**
 * Get a project by slug
 */
export async function getProjectBySlug(data: GetApiProjectsSlugBySlugData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsSlugBySlug({
    path: data.path,
  });
}

// =============================================================================
// PROJECT LIFECYCLE MANAGEMENT
// =============================================================================

/**
 * Publish a project
 */
export async function publishProject(data: PostApiProjectsByIdPublishData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProjectsByIdPublish({
    path: data.path,
  });
  
  // Revalidate projects cache
  revalidateTag('projects');
  
  return result;
}

/**
 * Unpublish a project
 */
export async function unpublishProject(data: PostApiProjectsByIdUnpublishData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProjectsByIdUnpublish({
    path: data.path,
  });
  
  // Revalidate projects cache
  revalidateTag('projects');
  
  return result;
}

/**
 * Archive a project
 */
export async function archiveProject(data: PostApiProjectsByIdArchiveData) {
  await configureAuthenticatedClient();
  
  const result = await postApiProjectsByIdArchive({
    path: data.path,
  });
  
  // Revalidate projects cache
  revalidateTag('projects');
  
  return result;
}

// =============================================================================
// PROJECT DISCOVERY & SEARCH
// =============================================================================

/**
 * Search projects
 */
export async function searchProjects(data?: GetApiProjectsSearchData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsSearch({
    query: data?.query,
  });
}

/**
 * Get popular projects
 */
export async function getPopularProjects(data?: GetApiProjectsPopularData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsPopular({
    query: data?.query,
  });
}

/**
 * Get recent projects
 */
export async function getRecentProjects(data?: GetApiProjectsRecentData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsRecent({
    query: data?.query,
  });
}

/**
 * Get featured projects
 */
export async function getFeaturedProjects(data?: GetApiProjectsFeaturedData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsFeatured({
    query: data?.query,
  });
}

/**
 * Get projects by category
 */
export async function getProjectsByCategory(data: GetApiProjectsCategoryByCategoryIdData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsCategoryByCategoryId({
    path: data.path,
    query: data.query,
  });
}

/**
 * Get projects by creator
 */
export async function getProjectsByCreator(data: GetApiProjectsCreatorByCreatorIdData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsCreatorByCreatorId({
    path: data.path,
    query: data.query,
  });
}

// =============================================================================
// PROJECT ANALYTICS
// =============================================================================

/**
 * Get project statistics
 */
export async function getProjectStatistics(data: GetApiProjectsByIdStatisticsData) {
  await configureAuthenticatedClient();
  
  return getApiProjectsByIdStatistics({
    path: data.path,
  });
}
