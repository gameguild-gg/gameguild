'use server';

import { revalidatePath, revalidateTag } from 'next/cache';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';
import { getJwtExpiryDate, isJwtExpired } from '@/lib/jwt-utils';

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
  slug: string; // Add slug for URL-friendly navigation
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
  slug: string; // Add slug for navigation
  status: 'not-started' | 'in-progress' | 'active' | 'on-hold';
  createdAt?: string;
  updatedAt?: string;
};

// Map CMS status to display status
function mapStatusToDisplay(cmsStatus: string | number): 'not-started' | 'in-progress' | 'active' | 'on-hold' {
  // Handle both string and numeric enum values
  if (typeof cmsStatus === 'number') {
    switch (cmsStatus) {
      case 0: // Draft
        return 'not-started';
      case 1: // UnderReview
        return 'in-progress';
      case 2: // Published
        return 'active';
      case 3: // Archived
        return 'on-hold';
      default:
        return 'not-started';
    }
  }
  
  // Handle string enum values (for backward compatibility)
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

// Map display status to CMS status enum value
function mapDisplayToCmsStatus(displayStatus: 'not-started' | 'in-progress' | 'active' | 'on-hold'): number {
  switch (displayStatus) {
    case 'not-started':
      return 0; // Draft
    case 'in-progress':
      return 1; // UnderReview
    case 'active':
      return 2; // Published
    case 'on-hold':
      return 3; // Archived
    default:
      return 0; // Draft
  }
}

// Map display visibility to CMS AccessLevel enum value
function mapDisplayToAccessLevel(visibility: 'Private' | 'Public' | 'Restricted'): number {
  switch (visibility) {
    case 'Private':
      return 0; // Private
    case 'Public':
      return 1; // Public
    case 'Restricted':
      return 2; // Restricted
    default:
      return 0; // Private
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
    error: (session as any)?.error
  });
  
  // Check for token refresh errors
  if ((session as any)?.error === 'RefreshTokenError') {
    console.error('🚨 Token refresh error detected - user needs to re-authenticate');
    throw new Error('Authentication token expired. Please sign in again.');
  }
  
  const accessToken = session?.accessToken as string;
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
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

// Helper function to make API calls with automatic token refresh on 401
async function makeApiCall(url: string, options: RequestInit = {}, retryCount = 0): Promise<Response> {
  const maxRetries = 1; // Only retry once to avoid infinite loops
  
  try {
    const headers = await getAuthHeaders();
    const response = await fetch(url, {
      ...options,
      headers: {
        ...headers,
        ...options.headers,
      },
    });
    
    // If we get a 401 and haven't retried yet, trigger a session refresh and retry
    if (response.status === 401 && retryCount < maxRetries) {
      console.warn('🔄 Got 401, attempting to refresh session and retry...');
      
      // Force NextAuth to refresh the session (this will trigger the jwt callback)
      const refreshedSession = await auth();
      
      if (refreshedSession?.accessToken && !(refreshedSession as any)?.error) {
        console.log('✅ Session refreshed successfully, retrying API call...');
        
        // Retry the API call with new session
        return makeApiCall(url, options, retryCount + 1);
      } else {
        console.error('❌ Session refresh failed or returned error');
        // If refresh failed, redirect to sign in
        if (typeof window !== 'undefined') {
          window.location.href = '/api/auth/signin';
        }
        throw new Error('Authentication failed. Please sign in again.');
      }
    }
    
    return response;
  } catch (error) {
    console.error('API call failed:', error);
    throw error;
  }
}

export async function getProjects(): Promise<ProjectListItem[]> {
  try {
    console.log('=== DEBUG: getProjects called ===');
    const timestamp = Date.now();
    console.log('Fetching projects from:', `${CMS_API_URL}/projects?_t=${timestamp}`);
    
    const response = await makeApiCall(`${CMS_API_URL}/projects?_t=${timestamp}`, {
      method: 'GET',
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
      slug: project.slug || project.id, // Use slug if available, fallback to ID
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
}) {  try {
    // Create a proper Project object that matches the C# model structure
    const cmsProjectData = {
      Title: projectData.title,  // Required field from ResourceBase
      Description: projectData.description || '',
      ShortDescription: projectData.shortDescription || '',
      Status: mapDisplayToCmsStatus(projectData.status || 'not-started'),  // ContentStatus enum as number
      Visibility: mapDisplayToAccessLevel(projectData.visibility || 'Private'),  // AccessLevel enum as number
      WebsiteUrl: projectData.websiteUrl || '',
      RepositoryUrl: projectData.repositoryUrl || '',
      SocialLinks: '',  // Required by Project model
      Slug: '', // Will be auto-generated by the controller
      Type: 0, // ProjectType.Game (default)
      DevelopmentStatus: 0, // DevelopmentStatus.Planning (default)
    };

    const response = await makeApiCall(`${CMS_API_URL}/projects`, {
      method: 'POST',
      body: JSON.stringify(cmsProjectData),
    });    if (!response.ok) {
      // Try to get detailed error information
      let errorMessage = `Failed to create project: ${response.statusText}`;
      try {
        const errorData = await response.text();
        console.error('Project creation error details:', errorData);
        errorMessage += ` - ${errorData}`;
      } catch (e) {
        console.error('Could not parse error response:', e);
      }
      throw new Error(errorMessage);    }

    const newProject: Project = await response.json();
    
    // Revalidate caches using the project's slug
    revalidatePath('/dashboard/projects');
    revalidatePath(`/dashboard/projects/${newProject.slug}`); // Use slug instead of ID
    revalidateTag('projects');
    revalidateTag(`project-${newProject.slug}`); // Use slug instead of ID
    
    return newProject;} catch (error) {
    console.error('Error creating project:', error);    // Return a fallback project if API is not available
    const fallbackProject: Project = {
      id: Date.now().toString(),
      title: projectData.title,
      slug: projectData.title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''), // Generate slug from title
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
    console.log('Fetching project by slug:', `${CMS_API_URL}/projects/slug/${slug}?_t=${timestamp}`);
    
    const response = await makeApiCall(`${CMS_API_URL}/projects/slug/${slug}?_t=${timestamp}`, {
      method: 'GET',
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
