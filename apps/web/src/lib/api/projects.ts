'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
    deleteApiProjectsById,
    getApiProjects,
    getApiProjectsById,
    getApiProjectsByIdStatistics,
    getApiProjectsFeatured,
    getApiProjectsPopular,
    getApiProjectsRecent,
    getApiProjectsSearch,
    postApiProjects,
    postApiProjectsByIdArchive,
    postApiProjectsByIdPublish,
    postApiProjectsByIdUnpublish,
    putApiProjectsById
} from '@/lib/api/generated/sdk.gen';
import type {
    Project as ApiProject
} from '@/lib/api/generated/types.gen';
import {
    AccessLevel,
    ContentStatus
} from '@/lib/api/generated/types.gen';

// Helper function to map API project to frontend project
function mapApiProjectToProject(apiProject: ApiProject): Project {
    return {
        id: apiProject.id || '',
        version: apiProject.version,
        name: apiProject.title || '',
        description: apiProject.description || '',
        longDescription: apiProject.shortDescription || '',
        category: apiProject.category?.name || '',
        gameVersion: '1.0.0', // Default or get from metadata
        status: mapContentStatusToStatus(apiProject.status),
        createdAt: apiProject.createdAt || new Date().toISOString(),
        updatedAt: apiProject.updatedAt || new Date().toISOString(),
        deletedAt: apiProject.deletedAt || undefined,
        isDeleted: apiProject.isDeleted || false,
        isPublic: apiProject.visibility === AccessLevel.PUBLIC,
        rating: 0, // Default rating, could be computed from feedback
        tags: apiProject.tags ? apiProject.tags.split(',').map(tag => tag.trim()) : [],
        sourceCodeUrl: apiProject.repositoryUrl || '',
        websiteUrl: apiProject.websiteUrl || '',
        screenshots: [], // Default empty array, could be from metadata
        systemRequirements: {
            minimum: 'Not specified',
            recommended: 'Not specified'
        },
        changelog: [],
        testingSessions: [],
        ownerId: '', // This would need to come from the API response
        ownerUsername: '',
        ownerName: ''
    };
}

// Helper function to map our status to API ContentStatus
function mapStatusToContentStatus(status: 'development' | 'beta' | 'released' | 'archived'): ContentStatus {
    switch (status) {
        case 'development': return ContentStatus.DRAFT;
        case 'beta': return ContentStatus.UNDER_REVIEW;
        case 'released': return ContentStatus.PUBLISHED;
        case 'archived': return ContentStatus.ARCHIVED;
        default: return ContentStatus.DRAFT;
    }
}

// Helper function to map API ContentStatus to our status
function mapContentStatusToStatus(status?: ContentStatus): 'development' | 'beta' | 'released' | 'archived' {
    switch (status) {
        case ContentStatus.DRAFT: return 'development';
        case ContentStatus.UNDER_REVIEW: return 'beta';
        case ContentStatus.PUBLISHED: return 'released';
        case ContentStatus.ARCHIVED: return 'archived';
        default: return 'development';
    }
}

// Extended interface for the frontend
export interface Project {
    id: string;
    version?: number;
    name: string;
    description: string;
    longDescription?: string;
    category: string;
    gameVersion?: string;
    status: 'development' | 'beta' | 'released' | 'archived';
    createdAt: string;
    updatedAt: string;
    deletedAt?: string;
    isDeleted: boolean;
    isPublic: boolean;
    rating?: number;
    tags: string[];
    sourceCodeUrl?: string;
    websiteUrl?: string;
    screenshots: string[];
    systemRequirements?: {
        minimum: string;
        recommended: string;
    };
    changelog?: Array<{
        version: string;
        date: string;
        changes: string[];
    }>;
    testingSessions?: Array<{
        id: string;
        title: string;
        status: 'pending' | 'active' | 'completed';
        participantCount: number;
        scheduledDate: string;
    }>;
    ownerId: string;
    ownerUsername?: string;
    ownerName?: string;
}

export interface CreateProjectRequest {
    name: string;
    description: string;
    longDescription?: string;
    category: string;
    gameVersion?: string;
    isPublic?: boolean;
    tags?: string[];
    sourceCodeUrl?: string;
    websiteUrl?: string;
}

export interface UpdateProjectRequest {
    name?: string;
    description?: string;
    longDescription?: string;
    category?: string;
    gameVersion?: string;
    isPublic?: boolean;
    tags?: string[];
    sourceCodeUrl?: string;
    websiteUrl?: string;
    expectedVersion?: number;
}

export interface ProjectSearchParams {
    query?: string;
    category?: string;
    status?: string;
    tags?: string[];
    sortBy?: 'name' | 'createdAt' | 'updatedAt' | 'rating';
    sortDirection?: 'asc' | 'desc';
    page?: number;
    pageSize?: number;
}

export async function getProjects(params?: ProjectSearchParams): Promise<Project[]> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjects({
            client,
            query: {
                query: params?.query,
                category: params?.category,
                status: params?.status ? mapStatusToContentStatus(params.status as any) : undefined,
                tags: params?.tags,
                sortBy: params?.sortBy,
                sortDirection: params?.sortDirection,
                page: params?.page,
                pageSize: params?.pageSize,
            }
        });

        if (response.error) {
            console.error('Error fetching projects:', response.error);
            return [];
        }

        const projects = Array.isArray(response.data) ? response.data : [];
        return projects.map(mapApiProjectToProject);
    } catch (error) {
        console.error('Error fetching projects:', error);
        return [];
    }
}

export async function getProjectById(id: string): Promise<Project | null> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjectsById({
            client,
            path: { id }
        });

        if (response.error) {
            console.error('Error fetching project:', response.error);
            return null;
        }

        return response.data || null;
    } catch (error) {
        console.error('Error fetching project:', error);
        return null;
    }
}

export async function createProject(project: CreateProjectRequest): Promise<Project | null> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await postApiProjects({
            client,
            body: project
        });

        if (response.error) {
            console.error('Error creating project:', response.error);
            return null;
        }

        return response.data || null;
    } catch (error) {
        console.error('Error creating project:', error);
        return null;
    }
}

export async function updateProject(id: string, updates: UpdateProjectRequest): Promise<Project | null> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await putApiProjectsById({
            client,
            path: { id },
            body: updates
        });

        if (response.error) {
            console.error('Error updating project:', response.error);
            return null;
        }

        return response.data || null;
    } catch (error) {
        console.error('Error updating project:', error);
        return null;
    }
}

export async function deleteProject(id: string): Promise<boolean> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await deleteApiProjectsById({
            client,
            path: { id }
        });

        if (response.error) {
            console.error('Error deleting project:', response.error);
            return false;
        }

        return true;
    } catch (error) {
        console.error('Error deleting project:', error);
        return false;
    }
}

export async function searchProjects(params: ProjectSearchParams): Promise<Project[]> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjectsSearch({
            client,
            query: {
                query: params.query,
                category: params.category,
                status: params.status,
                tags: params.tags,
                sortBy: params.sortBy,
                sortDirection: params.sortDirection,
                page: params.page,
                pageSize: params.pageSize,
            }
        });

        if (response.error) {
            console.error('Error searching projects:', response.error);
            return [];
        }

        return response.data?.data || [];
    } catch (error) {
        console.error('Error searching projects:', error);
        return [];
    }
}

export async function getPopularProjects(limit?: number): Promise<Project[]> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjectsPopular({
            client,
            query: { limit }
        });

        if (response.error) {
            console.error('Error fetching popular projects:', response.error);
            return [];
        }

        return response.data?.data || [];
    } catch (error) {
        console.error('Error fetching popular projects:', error);
        return [];
    }
}

export async function getRecentProjects(limit?: number): Promise<Project[]> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjectsRecent({
            client,
            query: { limit }
        });

        if (response.error) {
            console.error('Error fetching recent projects:', response.error);
            return [];
        }

        return response.data?.data || [];
    } catch (error) {
        console.error('Error fetching recent projects:', error);
        return [];
    }
}

export async function getFeaturedProjects(limit?: number): Promise<Project[]> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjectsFeatured({
            client,
            query: { limit }
        });

        if (response.error) {
            console.error('Error fetching featured projects:', response.error);
            return [];
        }

        return response.data?.data || [];
    } catch (error) {
        console.error('Error fetching featured projects:', error);
        return [];
    }
}

export async function getProjectStatistics(id: string): Promise<any> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await getApiProjectsByIdStatistics({
            client,
            path: { id }
        });

        if (response.error) {
            console.error('Error fetching project statistics:', response.error);
            return null;
        }

        return response.data || null;
    } catch (error) {
        console.error('Error fetching project statistics:', error);
        return null;
    }
}

export async function publishProject(id: string): Promise<boolean> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await postApiProjectsByIdPublish({
            client,
            path: { id }
        });

        if (response.error) {
            console.error('Error publishing project:', response.error);
            return false;
        }

        return true;
    } catch (error) {
        console.error('Error publishing project:', error);
        return false;
    }
}

export async function unpublishProject(id: string): Promise<boolean> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await postApiProjectsByIdUnpublish({
            client,
            path: { id }
        });

        if (response.error) {
            console.error('Error unpublishing project:', response.error);
            return false;
        }

        return true;
    } catch (error) {
        console.error('Error unpublishing project:', error);
        return false;
    }
}

export async function archiveProject(id: string): Promise<boolean> {
    const client = await configureAuthenticatedClient();

    try {
        const response = await postApiProjectsByIdArchive({
            client,
            path: { id }
        });

        if (response.error) {
            console.error('Error archiving project:', response.error);
            return false;
        }

        return true;
    } catch (error) {
        console.error('Error archiving project:', error);
        return false;
    }
}
