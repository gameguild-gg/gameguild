'use server';

import { getProjects, getProjectById, getProjectBySlug } from './projects.actions';
import type { GameProject } from '@/lib/types';
import { Project } from '@/lib/api/generated/types.gen';

/**
 * Get all projects
 */
export async function getAllProjectsService() {
  try {
    const response = await getProjects();

    if (response.data) {
      return { success: true, data: response.data };
    }

    return { success: false, error: 'No projects found' };
  } catch (error) {
    console.error('Error fetching projects:', error);
    return { success: false, error: 'Failed to fetch projects' };
  }
}

/**
 * Get a project by ID
 */
export async function getProjectByIdService(id: string) {
  try {
    const response = await getProjectById({
      path: { id },
    });

    if (response.data) {
      return { success: true, data: response.data };
    }

    return { success: false, error: 'Project not found' };
  } catch (error) {
    console.error('Error fetching project by ID:', error);
    return { success: false, error: 'Failed to fetch project' };
  }
}

/**
 * Get a project by slug
 */
export async function getProjectBySlugService(slug: string) {
  try {
    const response = await getProjectBySlug({
      path: { slug },
    });

    if (response.data) {
      return { success: true, data: response.data };
    }

    return { success: false, error: 'Project not found' };
  } catch (error) {
    console.error('Error fetching project by slug:', error);
    return { success: false, error: 'Failed to fetch project' };
  }
}
