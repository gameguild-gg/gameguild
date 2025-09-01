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
    return {
        id: apiProject.id || '',
        version: apiProject.version,
        name: apiProject.title,
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
        tags: apiProject.tags ? apiProject.tags.split(',') : [],
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
        ownerId: '', // Will need to be set from context
        ownerUsername: '', // Will need to be set from context
        ownerName: '' // Will need to be set from context
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

        const apiRequest: ApiUpdateProjectRequest = {
            title: updates.name,
            description: updates.description,
            shortDescription: updates.longDescription,
            repositoryUrl: updates.sourceCodeUrl,
            websiteUrl: updates.websiteUrl,
            tags: updates.tags || undefined,
            visibility: updates.isPublic !== undefined ?
                (updates.isPublic ? AccessLevel.PUBLIC : AccessLevel.PRIVATE) : undefined
        };

        const response = await putApiProjectsById({
            path: { id },
            body: apiRequest
        });

        if (response.data?.project) {
            return convertApiProjectToProject(response.data.project);
        }
        return null;
    } catch (error) {
        console.error(`Error updating project ${id}:`, error);
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
