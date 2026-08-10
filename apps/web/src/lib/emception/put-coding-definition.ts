'use server';

/**
 * Server action — PUTs the v2 coding definition for an existing assessment
 * via `PUT /v1.0/assessments/{id}/definition`. The backend re-runs the
 * Task 6 validator server-side; the caller is expected to validate
 * client-side first (see `apps/web/.../coding-definition/coding-definition-editor.tsx`).
 */

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
} from '@game-guild/client';
import { revalidatePath } from 'next/cache';

export type CodingLanguageId = 'cpp' | 'c' | 'sdl-cpp' | 'raylib-cpp';

/** Body shape sent to `PUT /v1.0/assessments/{id}/definition`. */
export interface CodingDefinitionPayload {
  kind: 'coding';
  language: CodingLanguageId;
  workspaceConfig: Record<string, unknown>;
  testPlan: {
    build?: Record<string, unknown>;
    cases: unknown[];
    timeoutMsPerCase?: number;
  };
  maxScore: number;
  passingScore: number;
  definitionSchemaVersion: 2;
}

export type PutResult =
  | { success: true }
  | { success: false; error: string };

function getApiClient() {
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function extractError(err: unknown): string {
  const e = err as
    | { status?: number; message?: string; detail?: string }
    | undefined;
  return e?.detail || e?.message || 'An unexpected error occurred.';
}

/**
 * Replace the v2 coding definition for `assessmentId`.
 *
 * Caller passes the fully-assembled payload; this helper wraps the
 * authenticated PUT and revalidates the assessment's routes so a
 * subsequent page load sees the new definition.
 */
export async function putCodingDefinition(
  assessmentId: string,
  definition: CodingDefinitionPayload,
  courseId?: string,
): Promise<PutResult> {
  try {
    const assessments = new GeneratedApi.LearningAssessmentsModule(getApiClient());
    const result = await assessments.putAssessmentsDefinition(assessmentId, {
      definitionSchemaVersion: 2,
      definition: definition as unknown as Record<string, unknown>,
    });

    if (!result.ok) {
      return { success: false, error: extractError(result.error) };
    }

    // Revalidate the assessment editor + coding-definition routes so a
    // subsequent navigation refetches the persisted definition.
    if (courseId) {
      revalidatePath(
        `/dashboard/learning/courses/${courseId}/assessments/${assessmentId}`,
      );
      revalidatePath(
        `/dashboard/learning/courses/${courseId}/assessments/${assessmentId}/coding-definition`,
      );
    }

    return { success: true };
  } catch (e) {
    return {
      success: false,
      error: `Unexpected error: ${e instanceof Error ? e.message : String(e)}`,
    };
  }
}
