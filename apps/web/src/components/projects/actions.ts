'use server';

import { revalidatePath, revalidateTag } from 'next/cache';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';

// Cache-busting utilities
export async function revalidateProjects() {
  revalidateTag('projects');
  revalidatePath('/dashboard/projects');
}

export async function revalidateProject(slug: string) {
  revalidateTag(`project-${slug}`);
  revalidatePath(`/dashboard/projects/${slug}`);
}

// Force refresh utility that can be called from client components
export async function forceRefreshProjects() {
  'use server';
  revalidateTag('projects');
  revalidatePath('/dashboard/projects', 'layout');
  revalidatePath('/dashboard/projects', 'page');
  // Also revalidate any project slug pages
  revalidatePath('/dashboard/projects/[slug]', 'page');
}

// Clear all project-related cache
export async function clearProjectCache() {
  'use server';
  revalidateTag('projects');
  revalidateTag('project-*'); // This won't work, but kept for reference
  revalidatePath('/dashboard/projects');
  revalidatePath('/dashboard/projects/[slug]');
}

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

// Helper function to get auth headers
async function getAuthHeaders() {
  const session = await auth();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  
  console.log('Session data:', {
    hasSession: !!session,
    hasAccessToken: !!session?.accessToken,
    userId: session?.user?.id,
    email: session?.user?.email,
    error: (session as any)?.error,
    tokenExpiry: session ? 'Available' : 'None'
  });
  
  // Check for token refresh errors
  if ((session as any)?.error === 'RefreshTokenError') {
    console.error('🚨 Token refresh error detected - user needs to re-authenticate');
    throw new Error('Authentication token expired. Please sign in again.');
  }
  
  if (session?.accessToken) {
    headers['Authorization'] = `Bearer ${session.accessToken}`;
    console.log('✅ Added Authorization header with token');
  } else {
    console.warn('⚠️ No access token found in session');
    if (session) {
      console.warn('Session exists but missing access token:', {
        hasUser: !!session.user,
        hasError: !!(session as any).error
      });
    }
  }
  
  return headers;
}

// Helper function to handle authentication errors
export async function handleAuthError(error: any, router?: any) {
  'use server';
  console.error('Authentication error:', error);
  
  if (error.message?.includes('Authentication token expired') || 
      error.message?.includes('401') ||
      error.message?.includes('Unauthorized')) {
    console.log('🔄 Authentication error detected, forcing re-login...');
    
    // Clear any cached session data
    revalidateTag('session');
    revalidatePath('/dashboard');
    
    // If we have a router, redirect to sign in
    if (router) {
      router.push('/api/auth/signin');
    }
    
    return { authError: true, message: 'Please sign in again' };
  }
  
  return { authError: false, message: error.message || 'Unknown error' };
}

export async function getProjects(): Promise<ProjectListItem[]> {
  try {
    console.log('=== DEBUG: getProjects called ===');
    const timestamp = Date.now();
    console.log('Fetching projects from:', `${CMS_API_URL}/projects?_t=${timestamp}`);
    
    const headers = await getAuthHeaders();
    console.log('Request headers:', headers);
    
    const response = await fetch(`${CMS_API_URL}/projects?_t=${timestamp}`, {
      method: 'GET',
      headers,
      cache: 'no-store', // Disable cache for dynamic user data
      next: { 
        revalidate: 0, // Additional Next.js cache busting
        tags: ['projects'] // Add cache tag for selective revalidation
      },
    });

    console.log('Response status:', response.status, response.statusText);

    if (!response.ok) {
      if (response.status === 401) {
        console.error('🚨 Unauthorized access - token may be expired');
        throw new Error('Authentication token expired. Please sign in again.');
      }
      console.error(`API request failed with status ${response.status}: ${response.statusText}`);
      const errorText = await response.text();
      console.error('Error response body:', errorText);
      throw new Error(`Failed to fetch projects: ${response.status} ${response.statusText}`);
    }

    const projects: Project[] = await response.json();
    console.log('✅ Successfully fetched projects:', projects.length);
    
    // Transform projects to match the expected format for the UI
    return projects.map(project => ({
      id: project.id,
      name: project.title,
      status: mapStatusToDisplay(project.status),
      createdAt: project.createdAt,
      updatedAt: project.updatedAt,
    }));} catch (error) {
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
    };

    const headers = await getAuthHeaders();
    const response = await fetch(`${CMS_API_URL}/projects`, {
      method: 'POST',
      headers,
      body: JSON.stringify(cmsProjectData),
    });

    if (!response.ok) {
      throw new Error(`Failed to create project: ${response.statusText}`);
    }    const newProject: Project = await response.json();
    
    // Revalidate caches
    revalidatePath('/dashboard/projects');
    revalidateTag('projects');
    
    return newProject;} catch (error) {
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
    const timestamp = Date.now();
    console.log('Fetching project by slug:', `${CMS_API_URL}/projects/slug/${slug}?_t=${timestamp}`);    const headers = await getAuthHeaders();
    const response = await fetch(`${CMS_API_URL}/projects/slug/${slug}?_t=${timestamp}`, {
      method: 'GET',
      headers,
      cache: 'no-store', // Disable cache for dynamic user data
      next: { 
        revalidate: 0, // Additional Next.js cache busting
        tags: [`project-${slug}`] // Add cache tag for selective revalidation
      },
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
