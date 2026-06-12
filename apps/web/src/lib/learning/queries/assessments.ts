import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type LearningAssessmentsAssessment,
  type LearningAssessmentsAssessmentType,
} from '@game-guild/client';
import { cache } from 'react';
import { learningApiGet } from './http';

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

function createAssessmentsModule() {
  return new GeneratedApi.LearningAssessmentsModule(getApiClient());
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
    const assessmentsModule = createAssessmentsModule();
    const result = await assessmentsModule.getAssessmentsCourse(courseId);
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
    const assessmentsModule = createAssessmentsModule();
    const result = await assessmentsModule.getAssessments(assessmentId);
    if (!result.ok) {
      return null;
    }
    return mapAssessment(result.data);
  } catch {
    return null;
  }
});

export interface CertificateTemplate {
  id: string;
  courseId: string;
  name: string;
  description: string | null;
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
  templateHtml: string;
  templateStyles: string | null;
}

interface CertificateTemplateApiDto {
  id: string;
  courseId: string;
  name: string;
  description?: string | null;
  isDefault?: boolean;
  isActive?: boolean;
  templateHtml?: string;
  templateStyles?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

interface CertificateApiDto {
  id: string;
  status?: string;
}

function mapCertificateTemplate(dto: CertificateTemplateApiDto, issuedCount = 0): CertificateTemplate {
  const createdAt = dto.createdAt ?? new Date().toISOString();

  return {
    id: dto.id,
    courseId: dto.courseId,
    name: dto.name,
    description: dto.description ?? null,
    status: dto.isActive === false ? 'archived' : 'active',
    issuedCount,
    createdAt,
    updatedAt: dto.updatedAt ?? createdAt,
  };
}

export const getCourseCertificates = cache(async (courseId: string): Promise<CourseCertificates> => {
  const [templates, issuedCertificates] = await Promise.all([
    learningApiGet<CertificateTemplateApiDto[]>(`/api/certificates/templates/course/${courseId}`, 120),
    learningApiGet<CertificateApiDto[]>(`/api/certificates/course/${courseId}`, 120),
  ]);

  const issuedCount = issuedCertificates?.length ?? 0;
  const mapped = (templates ?? []).map((template) => mapCertificateTemplate(template, issuedCount));

  return { templates: mapped, total: mapped.length, issuedCount };
});

export const getCertificateTemplate = cache(async (templateId: string): Promise<CertificateTemplateDetail | null> => {
  const template = await learningApiGet<CertificateTemplateApiDto>(`/api/certificates/templates/${templateId}`, 120);
  if (!template) return null;

  return {
    ...mapCertificateTemplate(template),
    previewUrl: `/api/certificates/templates/${template.id}`,
    templateHtml: template.templateHtml ?? '',
    templateStyles: template.templateStyles ?? null,
  };
});
