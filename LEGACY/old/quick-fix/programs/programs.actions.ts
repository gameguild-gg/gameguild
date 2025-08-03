import { revalidateTag, unstable_cache } from 'next/cache';
import { auth } from '@game-guild/web/src/auth';

// Types for Programs/Courses (same entity)
export interface Program {
  id: string;
  title: string;
  description?: string;
  shortDescription?: string;
  slug: string;
  contentType: string;
  visibility: 'Public' | 'Private' | 'Restricted';
  status: 'Draft' | 'Published' | 'Archived';
  imageUrl?: string;
  thumbnail?: string;
  difficulty?: string;
  duration?: number;
  price?: number;
  currency?: string;
  tags?: string[];
  category?: number; // API uses numeric category
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  tenantId: string;
  creatorId: string;
}

export interface ProgramFormData {
  title: string;
  description?: string;
  shortDescription?: string;
  contentType: string;
  visibility: 'Public' | 'Private' | 'Restricted';
  status: 'Draft' | 'Published' | 'Archived';
  imageUrl?: string;
  difficulty?: string;
  duration?: number;
  price?: number;
  currency?: string;
  tags?: string[];
  category?: number;
}

export interface ProgramsResponse {
  success: boolean;
  data?: Program[];
  error?: string;
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface ProgramStatistics {
  totalPrograms: number;
  publishedPrograms: number;
  draftPrograms: number;
  archivedPrograms: number;
  totalViews: number;
  totalEnrollments: number;
  averageRating: number;
  programsCreatedToday: number;
  programsCreatedThisWeek: number;
  programsCreatedThisMonth: number;
}

// Cache configuration
const CACHE_TAGS = {
  PROGRAMS: 'programs',
  PROGRAM_DETAIL: 'program-detail',
  PROGRAM_STATISTICS: 'program-statistics',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get programs with authentication
 */
export async function getPrograms(page: number = 1, limit: number = 20, status?: string, visibility?: string): Promise<ProgramsResponse> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const queryParams = new URLSearchParams({
      skip: ((page - 1) * limit).toString(),
      take: limit.toString(),
      ...(status && { status }),
      ...(visibility && { visibility }),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/program?${queryParams}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PROGRAMS],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const programs: Program[] = await response.json();

    return {
      success: true,
      data: programs,
      pagination: {
        page,
        limit,
        total: programs.length, // TODO: Get from headers if API provides it
        totalPages: Math.ceil(programs.length / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching programs:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get published programs (public access)
 */
export async function getPublishedPrograms(page: number = 1, limit: number = 20): Promise<ProgramsResponse> {
  try {
    const queryParams = new URLSearchParams({
      skip: ((page - 1) * limit).toString(),
      take: limit.toString(),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/program/published?${queryParams}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PROGRAMS],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const programs: Program[] = await response.json();

    return {
      success: true,
      data: programs,
      pagination: {
        page,
        limit,
        total: programs.length,
        totalPages: Math.ceil(programs.length / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching published programs:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get single program by ID
 */
export async function getProgram(id: string): Promise<{ success: boolean; data?: Program; error?: string }> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/program/${id}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PROGRAM_DETAIL, `program-${id}`],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const program: Program = await response.json();
    return { success: true, data: program };
  } catch (error) {
    console.error('Error fetching program:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get program by slug
 */
export async function getProgramBySlug(slug: string): Promise<{ success: boolean; data?: Program; error?: string }> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/program/slug/${slug}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PROGRAM_DETAIL, `program-slug-${slug}`],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const program: Program = await response.json();
    return { success: true, data: program };
  } catch (error) {
    console.error('Error fetching program by slug:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get program statistics
 */
export async function getProgramStatistics(): Promise<{ success: boolean; data?: ProgramStatistics; error?: string }> {
  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/program/statistics`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.PROGRAM_STATISTICS],
      },
    });

    if (!response.ok) {
      // If endpoint doesn't exist, return mock data
      if (response.status === 404) {
        return {
          success: true,
          data: {
            totalPrograms: 0,
            publishedPrograms: 0,
            draftPrograms: 0,
            archivedPrograms: 0,
            totalViews: 0,
            totalEnrollments: 0,
            averageRating: 0,
            programsCreatedToday: 0,
            programsCreatedThisWeek: 0,
            programsCreatedThisMonth: 0,
          },
        };
      }

      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const statistics: ProgramStatistics = await response.json();
    return { success: true, data: statistics };
  } catch (error) {
    console.error('Error fetching program statistics:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Create a new program
 */
export async function createProgram(formData: ProgramFormData): Promise<{ success: boolean; data?: Program; error?: string }> {
  'use server';

  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/programs`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      body: JSON.stringify(formData),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const program: Program = await response.json();

    // Revalidate cache
    revalidateTag(CACHE_TAGS.PROGRAMS);
    revalidateTag(CACHE_TAGS.PROGRAM_STATISTICS);

    return { success: true, data: program };
  } catch (error) {
    console.error('Error creating program:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Update an existing program
 */
export async function updateProgram(id: string, formData: ProgramFormData): Promise<{ success: boolean; data?: Program; error?: string }> {
  'use server';

  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/programs/${id}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      body: JSON.stringify(formData),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const program: Program = await response.json();

    // Revalidate cache
    revalidateTag(CACHE_TAGS.PROGRAMS);
    revalidateTag(CACHE_TAGS.PROGRAM_DETAIL);
    revalidateTag(`program-${id}`);
    revalidateTag(CACHE_TAGS.PROGRAM_STATISTICS);

    return { success: true, data: program };
  } catch (error) {
    console.error('Error updating program:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Delete a program
 */
export async function deleteProgram(id: string): Promise<{ success: boolean; error?: string }> {
  'use server';

  try {
    const session = await auth();
    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/programs/${id}`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    // Revalidate cache
    revalidateTag(CACHE_TAGS.PROGRAMS);
    revalidateTag(CACHE_TAGS.PROGRAM_DETAIL);
    revalidateTag(`program-${id}`);
    revalidateTag(CACHE_TAGS.PROGRAM_STATISTICS);

    return { success: true };
  } catch (error) {
    console.error('Error deleting program:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Server-only function to refresh programs cache
 */
export async function refreshPrograms(): Promise<void> {
  'use server';
  revalidateTag(CACHE_TAGS.PROGRAMS);
  revalidateTag(CACHE_TAGS.PROGRAM_STATISTICS);
}

/**
 * Cached version of getPrograms for better performance
 */
export const getCachedPrograms = unstable_cache(
  async (page: number, limit: number, status?: string, visibility?: string) => {
    return getPrograms(page, limit, status, visibility);
  },
  ['programs'],
  {
    tags: [CACHE_TAGS.PROGRAMS],
    revalidate: REVALIDATION_TIME,
  },
);
