'use server';

import { revalidateTag } from 'next/cache';
import { auth } from '@/auth';
import { environment } from '@/configs/environment';
import {
  getApiProjects,
  postApiProjects,
  getApiProjectsById,
  putApiProjectsById,
  deleteApiProjectsById,
  postApiProjectsByIdPublish,
  postApiProjectsByIdUnpublish,
  postApiProjectsByIdArchive,
  getApiProjectsSearch,
  getApiProjectsPopular,
  getApiProjectsRecent,
  getApiProjectsFeatured,
} from '@/lib/api/generated/sdk.gen';
import type { Project, PostApiProjectsData, PutApiProjectsByIdData } from '@/lib/api/generated/types.gen';

// Get all projects with optional filtering
export async function getProjectsData(params?: {
  searchTerm?: string;
  categoryId?: string;
  creatorId?: string;
  type?: string;
  status?: string;
  visibility?: number;
  skip?: number;
  take?: number;
}) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getApiProjects({
      baseUrl: environment.apiBaseUrl,
      query: params as any,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch projects');
    }

    return {
      projects: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching projects:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch projects');
  }
}

// Get a single project by ID
export async function getProjectById(id: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getApiProjectsById({
      path: { id },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Project not found');
    }

    return response.data as Project;
  } catch (error) {
    console.error('Error fetching project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch project');
  }
}

// Create a new project
export async function createProject(projectData: {
  title: string;
  description?: string;
  shortDescription?: string;
  visibility: number;
  category?: string;
  type?: string;
  developmentStatus?: string;
  websiteUrl?: string;
  repositoryUrl?: string;
  downloadUrl?: string;
  tags?: string[];
  imageUrl?: string;
}) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postApiProjects({
      body: {
        ...projectData,
        tags: projectData.tags?.join(',')
      } as PostApiProjectsData['body'],
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.data) {
      throw new Error('Failed to create project');
    }

    // Revalidate projects cache
    revalidateTag('projects');
    
    return response.data as Project;
  } catch (error) {
    console.error('Error creating project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create project');
  }
}

// Update an existing project
export async function updateProject(id: string, projectData: {
  title?: string;
  description?: string;
  shortDescription?: string;
  visibility?: number;
  category?: string;
  type?: string;
  developmentStatus?: string;
  websiteUrl?: string;
  repositoryUrl?: string;
  downloadUrl?: string;
  tags?: string[];
  imageUrl?: string;
}) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await putApiProjectsById({
      path: { id },
      body: {
        ...projectData,
        tags: projectData.tags?.join(',')
      } as PutApiProjectsByIdData['body'],
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.data) {
      throw new Error('Failed to update project');
    }

    // Revalidate projects cache
    revalidateTag('projects');
    
    return response.data as Project;
  } catch (error) {
    console.error('Error updating project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update project');
  }
}

// Delete a project
export async function deleteProject(id: string) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    await deleteApiProjectsById({
      path: { id },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`
      }
    });

    // Revalidate projects cache
    revalidateTag('projects');
    
    return { success: true };
  } catch (error) {
    console.error('Error deleting project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete project');
  }
}

// Publish a project
export async function publishProject(id: string) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postApiProjectsByIdPublish({
      path: { id },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`
      }
    });

    // Revalidate projects cache
    revalidateTag('projects');
    
    return response.data;
  } catch (error) {
    console.error('Error publishing project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to publish project');
  }
}

// Unpublish a project
export async function unpublishProject(id: string) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postApiProjectsByIdUnpublish({
      path: { id },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`
      }
    });

    // Revalidate projects cache
    revalidateTag('projects');
    
    return response.data;
  } catch (error) {
    console.error('Error unpublishing project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to unpublish project');
  }
}

// Archive a project
export async function archiveProject(id: string) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postApiProjectsByIdArchive({
      path: { id },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`
      }
    });

    // Revalidate projects cache
    revalidateTag('projects');
    
    return response.data;
  } catch (error) {
    console.error('Error archiving project:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to archive project');
  }
}

// Search projects
export async function searchProjects(query: string, filters?: {
  category?: string;
  type?: string;
  status?: string;
}) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getApiProjectsSearch({
      query: { q: query, ...filters },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store'
      }
    });

    return response.data as { projects: Project[]; total: number; };
  } catch (error) {
    console.error('Error searching projects:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search projects');
  }
}

// Get popular projects
export async function getPopularProjects(limit = 10) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getApiProjectsPopular({
      query: { limit },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store'
      }
    });

    return response.data as { projects: Project[]; };
  } catch (error) {
    console.error('Error fetching popular projects:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch popular projects');
  }
}

// Get recent projects
export async function getRecentProjects(limit = 10) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getApiProjectsRecent({
      query: { limit },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store'
      }
    });

    return response.data as { projects: Project[]; };
  } catch (error) {
    console.error('Error fetching recent projects:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch recent projects');
  }
}

// Get featured projects
export async function getFeaturedProjects(limit = 10) {
  const session = await auth();
  
  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getApiProjectsFeatured({
      query: { limit },
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store'
      }
    });

    return response.data as { projects: Project[]; };
  } catch (error) {
    console.error('Error fetching featured projects:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch featured projects');
  }
}
