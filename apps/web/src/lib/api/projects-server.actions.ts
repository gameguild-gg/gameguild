'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
    deleteApiProjectsById,
    getApiProjects,
    getApiProjectsById,
    postApiProjects,
    putApiProjectsById
} from '@/lib/api/generated/sdk.gen';
import type {
    CreateProjectRequest as ApiCreateProjectRequest,
    Project as ApiProject,
    UpdateProjectRequest as ApiUpdateProjectRequest
} from '@/lib/api/generated/types.gen';
import {
    AccessLevel,
    ContentStatus
} from '@/lib/api/generated/types.gen';
import type { CreateProjectRequest, Project, ProjectSearchParams, UpdateProjectRequest } from './projects-simple';

/**
 * Convert ContentStatus to our status string
 */
function convertContentStatus(status?: ContentStatus): 'development' | 'beta' | 'released' | 'archived' {
    switch (status) {
        case ContentStatus.PUBLISHED:
            return 'released';
        case ContentStatus.UNDER_REVIEW:
            return 'beta';
        case ContentStatus.ARCHIVED:
            return 'archived';
        case ContentStatus.DRAFT:
        default:
            return 'development';
    }
}

/**
 * Convert API Project to our Project interface
 */
function convertApiProjectToProject(apiProject: ApiProject): Project {
    // Handle tags - API stores as string, we need array
    let tagsArray: string[] = [];
    if (apiProject.tags && typeof apiProject.tags === 'string') {
        tagsArray = apiProject.tags.split(',').map(tag => tag.trim()).filter(tag => tag.length > 0);
    }

    return {
        id: apiProject.id || '',
        version: apiProject.version,
        name: apiProject.title || '',
        description: apiProject.description || '',
        longDescription: apiProject.shortDescription || undefined,
        category: apiProject.category?.name || '',
        gameVersion: '1.0.0', // Not available in API, use default
        status: convertContentStatus(apiProject.status),
        createdAt: apiProject.createdAt || '',
        updatedAt: apiProject.updatedAt || '',
        deletedAt: apiProject.deletedAt || undefined,
        isDeleted: apiProject.isDeleted || false,
        isPublic: apiProject.visibility === AccessLevel.PUBLIC,
        rating: 0, // Not available in API
        tags: tagsArray,
        downloadUrl: apiProject.downloadUrl || undefined,
        sourceCodeUrl: apiProject.repositoryUrl || undefined,
        websiteUrl: apiProject.websiteUrl || undefined,
        screenshots: [], // Not available in API
        systemRequirements: {
            minimum: 'Not specified',
            recommended: 'Not specified'
        },
        changelog: [],
        testingSessions: [],
        ownerId: apiProject.createdById || '', // Use createdById as ownerId
        ownerUsername: apiProject.createdBy?.username || '', // Extract from createdBy object
        ownerName: apiProject.createdBy?.name || '' // Extract from createdBy object
    };
}

/**
 * Get all projects with optional filtering
 */
export async function getProjects(params?: ProjectSearchParams): Promise<Project[]> {
    try {
        await configureAuthenticatedClient();

        // Build query parameters for the API call
        const queryParams: any = {};
        if (params?.query) queryParams.searchTerm = params.query;
        if (params?.sortBy) queryParams.sortBy = params.sortBy;
        if (params?.sortDirection) queryParams.sortDirection = params.sortDirection;
        if (params?.page) queryParams.skip = (params.page - 1) * (params.pageSize || 10);
        if (params?.pageSize) queryParams.take = params.pageSize;

        const response = await getApiProjects({
            query: queryParams
        });

        const projects = response.data || [];
        console.log(`Fetched ${projects.length} projects from API`);
        return projects.map(convertApiProjectToProject);
    } catch (error) {
        console.error('Error fetching projects:', error);
        // Return empty array to avoid breaking the UI
        return [];
    }
}

/**
 * Get a project by ID
 */
export async function getProjectById(id: string): Promise<Project | null> {
    try {
        await configureAuthenticatedClient();

        const response = await getApiProjectsById({
            path: { id }
        });

        if (response.data) {
            return convertApiProjectToProject(response.data);
        }
        return null;
    } catch (error) {
        console.error(`Error fetching project ${id}:`, error);
        // Log additional details for debugging
        if (error instanceof Error) {
            console.error('Error details:', {
                message: error.message,
                stack: error.stack
            });
        }
        return null;
    }
}

/**
 * Get projects by user ID or username
 */
export async function getProjectsByUser(userId: string): Promise<Project[]> {
    try {
        await configureAuthenticatedClient();

        // Use search API - for now get all projects and filter client-side
        // TODO: Update when API supports owner filtering
        const response = await getApiProjects({
            query: {}
        });

        const projects = response.data || [];
        return projects.map(convertApiProjectToProject);
    } catch (error) {
        console.error(`Error fetching projects for user ${userId}:`, error);
        // Return empty array to avoid breaking the UI
        return [];
    }
}

/**
 * Create a new project
 */
export async function createProject(project: CreateProjectRequest): Promise<Project | null> {
    try {
        await configureAuthenticatedClient();

        const apiRequest: ApiCreateProjectRequest = {
            title: project.name,
            description: project.description,
            shortDescription: project.longDescription,
            repositoryUrl: project.sourceCodeUrl,
            websiteUrl: project.websiteUrl,
            tags: project.tags || [],
            visibility: project.isPublic ? AccessLevel.PUBLIC : AccessLevel.PRIVATE
        };

        const response = await postApiProjects({
            body: apiRequest
        });

        if (response.data?.project) {
            return convertApiProjectToProject(response.data.project);
        }
        return null;
    } catch (error) {
        console.error('Error creating project:', error);
        return null;
    }
}

/**
 * Update an existing project
 */
export async function updateProject(id: string, updates: UpdateProjectRequest): Promise<Project | null> {
    try {
        await configureAuthenticatedClient();

        // Convert tags to array format expected by UpdateProjectRequest
        let tagsArray: string[] | undefined = undefined;
        if (updates.tags && Array.isArray(updates.tags)) {
            tagsArray = updates.tags.filter(tag => tag.trim().length > 0);
        }

        const apiRequest: ApiUpdateProjectRequest = {
            title: updates.name,
            description: updates.description,
            shortDescription: updates.longDescription,
            repositoryUrl: updates.sourceCodeUrl,
            websiteUrl: updates.websiteUrl,
            tags: tagsArray,
            visibility: updates.isPublic !== undefined ?
                (updates.isPublic ? AccessLevel.PUBLIC : AccessLevel.PRIVATE) : undefined
        };

        console.log('Updating project:', id, 'with data:', apiRequest);

        const response = await putApiProjectsById({
            path: { id },
            body: apiRequest
        });

        console.log('Update response:', response);

        if (response.data?.project) {
            return convertApiProjectToProject(response.data.project);
        }

        // Check if the response has error data
        if (response.error) {
            console.error('API update error:', response.error);
        }

        return null;
    } catch (error) {
        console.error(`Error updating project ${id}:`, error);
        // Log more detailed error information
        if (error instanceof Error) {
            console.error('Error message:', error.message);
            console.error('Error stack:', error.stack);
        }
        return null;
    }
}

/**
 * Delete a project
 */
export async function deleteProject(id: string): Promise<boolean> {
    try {
        await configureAuthenticatedClient();

        await deleteApiProjectsById({
            path: { id }
        });

        return true;
    } catch (error) {
        console.error(`Error deleting project ${id}:`, error);
        return false;
    }
}
