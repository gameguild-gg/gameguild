// =============================================================================
// COURSES LIST DATA QUERIES
// =============================================================================

import { createServerClient } from '@game-guild/client';
import { getToken } from '@/auth';

/**
 * Course list item with data for KPI computation
 */
export interface CourseListItem {
  id: string;
  title: string;
  thumbnail: string | null;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'unlisted';
  enrollments: Array<{ id: string; completedAt: string | null }>;
  ratings: Array<{ score: number }>;
}

// Shape returned by GET /v1/courses (ProgramDto)
interface ProgramDto {
  id: string;
  title: string;
  thumbnail: string | null;
  status: string; // ContentStatus enum
  visibility: string; // ContentVisibility enum
  currentEnrollments: number;
  averageRating: number;
  totalRatings: number;
}

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

// ContentStatus enum: Draft=0, Review=1, Published=2, Archived=3, Deleted=4
function mapStatus(status: unknown): 'draft' | 'published' | 'archived' {
  if (status === 2 || status === '2') return 'published';
  if (status === 3 || status === '3') return 'archived';
  const s = String(status ?? '').toLowerCase();
  if (s === 'published') return 'published';
  if (s === 'archived') return 'archived';
  return 'draft';
}

// ContentVisibility enum: Private=0, Internal=1, Friends=2, Protected=3, Public=4
function mapVisibility(visibility: unknown): 'public' | 'private' | 'unlisted' {
  if (visibility === 4 || visibility === '4') return 'public';
  const v = String(visibility ?? '').toLowerCase();
  if (v === 'public') return 'public';
  if (v === 'unlisted') return 'unlisted';
  return 'private';
}

/**
 * Fetch courses list for the instructor.
 *
 * Endpoint: GET /v1/courses
 */
export async function getCourses(): Promise<{
  courses: CourseListItem[];
  error: string | null;
}> {
  try {
    const client = getApiClient();
    const result = await client.request<ProgramDto[]>({
      method: 'GET',
      path: '/v1/courses',
      params: { take: 50 },
      requiresAuth: true,
    });

    if (result.ok && Array.isArray(result.data)) {
      return {
        courses: result.data.map((p) => ({
          id: p.id,
          title: p.title,
          thumbnail: p.thumbnail,
          status: mapStatus(p.status),
          visibility: mapVisibility(p.visibility),
          enrollments: Array.from({ length: p.currentEnrollments }, (_, i) => ({ id: `e${i}`, completedAt: null })),
          ratings: Array.from({ length: p.totalRatings }, () => ({
            score: p.averageRating || 0,
          })),
        })),
        error: null,
      };
    }

    const err = result.error as { status?: number; code?: string; message?: string; detail?: string } | undefined;
    return {
      courses: [],
      error: `[${err?.status ?? 'unknown'}${err?.code ? ' ' + err.code : ''}] ${err?.detail || err?.message || 'Failed to load courses'}`,
    };
  } catch (e) {
    return {
      courses: [],
      error: `Unexpected: ${e instanceof Error ? e.message : String(e)}`,
    };
  }
}
