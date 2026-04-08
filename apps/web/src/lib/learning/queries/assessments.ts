import { cache } from 'react';
import {
  createServerClient,
  type LearningAssessmentsAssessment,
  type LearningAssessmentsAssessmentType,
} from '@game-guild/client';
import { getToken } from '@/auth';

// =============================================================================
// API CLIENT
// =============================================================================

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

// =============================================================================
// TYPES (re-exported for convenience)
// =============================================================================

export type AssessmentType = LearningAssessmentsAssessmentType;

export interface Assessment {
  id: string;
  courseId: string;
  contentId: string | null;
  title: string;
  description: string | null;
  type: AssessmentType;
  maxScore: number;
  passingScore: number;
  timeLimitMinutes: number | null;
  maxAttempts: number | null;
  isRequired: boolean;
  order: number;
  availableFrom: string | null;
  availableUntil: string | null;
  isAvailable: boolean;
}

export interface CourseAssessments {
  assessments: Assessment[];
  total: number;
}

// =============================================================================
// MAPPERS
// =============================================================================

function mapAssessment(dto: LearningAssessmentsAssessment): Assessment {
  return {
    id: dto.id ?? '',
    courseId: dto.courseId ?? '',
    contentId: dto.contentId ?? null,
    title: dto.title ?? '',
    description: dto.description ?? null,
    type: dto.type ?? 'Quiz',
    maxScore: dto.maxScore ?? 100,
    passingScore: dto.passingScore ?? 70,
    timeLimitMinutes: dto.timeLimitMinutes ?? null,
    maxAttempts: dto.maxAttempts ?? null,
    isRequired: dto.isRequired ?? true,
    order: dto.order ?? 0,
    availableFrom: dto.availableFrom ?? null,
    availableUntil: dto.availableUntil ?? null,
    isAvailable: dto.isAvailable ?? true,
  };
}

// =============================================================================
// FETCH FUNCTIONS
// =============================================================================

/**
 * Fetch course assessments (conditional: hasAssessments).
 */
export const getCourseAssessments = cache(async (courseId: string): Promise<CourseAssessments> => {
  try {
    const client = getApiClient();
    const result = await client.request<LearningAssessmentsAssessment[]>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}`,
      requiresAuth: true,
    });
    if (!result.ok) {
      console.error('Failed to fetch assessments:', result.error);
      return { assessments: [], total: 0 };
    }
    const assessments = (result.data ?? []).map(mapAssessment);
    return { assessments, total: assessments.length };
  } catch (err) {
    console.error('Error fetching course assessments:', err);
    return { assessments: [], total: 0 };
  }
});

/**
 * Fetch single assessment by ID.
 */
export const getAssessment = cache(async (assessmentId: string): Promise<Assessment | null> => {
  try {
    const client = getApiClient();
    const result = await client.request<LearningAssessmentsAssessment>({
      method: 'GET',
      path: `/v1/assessments/${assessmentId}`,
      requiresAuth: true,
    });
    if (!result.ok) {
      return null;
    }
    return mapAssessment(result.data);
  } catch {
    return null;
  }
});

// =============================================================================
// CERTIFICATE STUBS (module not yet enabled)
// =============================================================================

export interface CertificateTemplate {
  id: string;
  courseId: string;
  name: string;
  description: string;
  status: 'draft' | 'active' | 'archived';
  issuedCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface CourseCertificates {
  templates: CertificateTemplate[];
  total: number;
  issuedCount: number;
}

export interface CertificateTemplateDetail extends CertificateTemplate {
  previewUrl: string;
}

export const getCourseCertificates = cache(async (courseId: string): Promise<CourseCertificates> => {
  void courseId;
  return { templates: [], total: 0, issuedCount: 0 };
});

export const getCertificateTemplate = cache(async (templateId: string): Promise<CertificateTemplateDetail | null> => {
  void templateId;
  return null;
});
