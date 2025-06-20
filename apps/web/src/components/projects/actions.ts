'use server';

import { revalidatePath } from 'next/cache';
import { environment } from '@/configs/environment';

export type Project = {
  id: string;
  title: string;
  description?: string;
  shortDescription?: string;
  status: 'Draft' | 'UnderReview' | 'Published' | 'Archived';
  category?: {
    id: string;
    name: string;
  };
  websiteUrl?: string;
  repositoryUrl?: string;
  socialLinks?: string;
  visibility: 'Private' | 'Public' | 'Restricted';
  createdAt?: string;
  updatedAt?: string;
  createdBy?: {
    id: string;
    name: string;
  };
};

export type ProjectListItem = {
  id: string;
  name: string;
  status: 'not-started' | 'in-progress' | 'active' | 'on-hold';
  createdAt?: string;
  updatedAt?: string;
};

// Map CMS status to display status
function mapStatusToDisplay(cmsStatus: string): 'not-started' | 'in-progress' | 'active' | 'on-hold' {
  switch (cmsStatus) {
    case 'Draft':
      return 'not-started';
    case 'UnderReview':
      return 'in-progress';
    case 'Published':
      return 'active';
    case 'Archived':
      return 'on-hold';
    default:
      return 'not-started';
  }
}

// Map display status to CMS status
function mapDisplayToCmsStatus(displayStatus: 'not-started' | 'in-progress' | 'active' | 'on-hold'): string {
  switch (displayStatus) {
    case 'not-started':
      return 'Draft';
    case 'in-progress':
      return 'UnderReview';
    case 'active':
      return 'Published';
    case 'on-hold':
      return 'Archived';
    default:
      return 'Draft';
  }
}

const CMS_API_URL = environment.apiBaseUrl || 'http://localhost:5001';

export async function getProjects(): Promise<ProjectListItem[]> {
  try {
    console.log('Fetching projects from:', `${CMS_API_URL}/projects`);
    
    const response = await fetch(`${CMS_API_URL}/projects`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
      cache: 'no-store', // Ensure fresh data for server actions
    });

    console.log('Response status:', response.status, response.statusText);

    if (!response.ok) {
      console.error(`API request failed with status ${response.status}: ${response.statusText}`);
      throw new Error(`Failed to fetch projects: ${response.status} ${response.statusText}`);
    }

    const projects: Project[] = await response.json();
    
    // Transform projects to match the expected format for the UI
    return projects.map(project => ({
      id: project.id,
      name: project.title,
      status: mapStatusToDisplay(project.status),
      createdAt: project.createdAt,
      updatedAt: project.updatedAt,
    }));  } catch (error) {
    console.error('Error fetching projects:', error);
    throw error; // Re-throw the error instead of returning fallback data
  }
}

export async function createProject(projectData: {
  title: string;
  description?: string;
  shortDescription?: string;
  websiteUrl?: string;
  repositoryUrl?: string;
  status?: 'not-started' | 'in-progress' | 'active' | 'on-hold';
  visibility?: 'Private' | 'Public' | 'Restricted';
}) {
  try {
    const cmsProjectData = {
      title: projectData.title,
      description: projectData.description || '',
      shortDescription: projectData.shortDescription || '',
      status: mapDisplayToCmsStatus(projectData.status || 'not-started'),
      visibility: projectData.visibility || 'Private',
      websiteUrl: projectData.websiteUrl || '',
      repositoryUrl: projectData.repositoryUrl || '',
      socialLinks: '',
    };    const response = await fetch(`${CMS_API_URL}/projects`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(cmsProjectData),
    });

    if (!response.ok) {
      throw new Error(`Failed to create project: ${response.statusText}`);
    }    const newProject: Project = await response.json();
    revalidatePath('/projects');
    
    return newProject;  } catch (error) {
    console.error('Error creating project:', error);
    // Return a fallback project if API is not available
    const fallbackProject: Project = {
      id: Date.now().toString(),
      title: projectData.title,
      status: 'Draft',
      visibility: 'Private',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    revalidatePath('/projects');
    return fallbackProject;
  }
}

export async function getProjectBySlug(slug: string): Promise<Project | null> {
  try {
    console.log('Fetching project by slug:', `${CMS_API_URL}/projects/slug/${slug}`);
    
    const response = await fetch(`${CMS_API_URL}/projects/slug/${slug}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
      cache: 'no-store',
    });

    console.log('Response status:', response.status, response.statusText);

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch project: ${response.status} ${response.statusText}`);
    }

    const project: Project = await response.json();
    return project;
  } catch (error) {
    console.error('Error fetching project by slug:', error);
    throw error;
  }
}
