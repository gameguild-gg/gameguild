/**
 * Typed wrappers for the v1 coding-assignment endpoints on
 * `LearningCoursesProgramcontent`:
 *
 *   GET  /v1.0/courses/{programId}/content/{contentId}/coding-assignment         (student/public)
 *   GET  /v1.0/courses/{programId}/content/{contentId}/coding-assignment/full     (instructor/full)
 *   PUT  /v1.0/courses/{programId}/content/{contentId}/coding-assignment         (instructor/upsert)
 *
 * Bypasses the generated module methods because the codegen Zod schema strips
 * unknown keys, which would silently drop the polymorphic `Test` variant
 * fields (`Stdin`/`Stdout`/`Function`/`Result`). Going through
 * `client.request<unknown>()` + the runtime guards in `./types.ts` keeps the
 * typed shape intact.
 */

import { getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';
import {
  isRecord,
  narrowCodingAssignmentContent,
  type CodingAssignmentContent,
} from './types';

export type {
  CodingAssignmentContent,
  CodingEnvironment,
  WorkspaceData,
  TestSuite,
  Test,
  StandardTest,
  FunctionalTest,
  TestFunctionData,
  FunctionParameter,
  FunctionParameterWithName,
  FunctionParameterType,
  FunctionParameterValue,
  BundleFileMeta,
  GradingConfig,
  FileVisibility,
  FileEncoding,
  TestKind,
} from './types';

export {
  isStandardTest,
  isFunctionalTest,
  isTest,
  isTestFunctionData,
  isFunctionParameter,
  isFunctionParameterWithName,
  isRecord,
  narrowCodingAssignmentContent,
  TEST_KIND,
} from './types';

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

export type PutCodingAssignmentResult =
  | { success: true }
  | { success: false; error: string };

function extractError(err: unknown): string {
  if (!isRecord(err)) return 'An unexpected error occurred.';
  const detail = typeof err.detail === 'string' ? err.detail : undefined;
  const message = typeof err.message === 'string' ? err.message : undefined;
  return detail || message || 'An unexpected error occurred.';
}

/**
 * Student-facing fetch of a coding assignment.
 *
 * Hits `GET /v1.0/courses/{programId}/content/{contentId}/coding-assignment`.
 * Backend enrolls the caller via `HasStudentAccessAsync` and strips Private
 * tests + Private files before responding.
 *
 * Returns the typed content, or `null` on any failure (404, 403, network,
 * shape mismatch) — callers treat null as "no content available".
 */
export async function getCodingAssignmentPublic(
  programId: string,
  contentId: string,
): Promise<CodingAssignmentContent | null> {
  try {
    const client = getApiClient();
    const result = await client.request<unknown>({
      method: 'GET',
      path: `/v1.0/courses/${programId}/content/${contentId}/coding-assignment`,
      requiresAuth: true,
    });
    if (!result.ok) return null;
    return narrowCodingAssignmentContent(result.data);
  } catch (err) {
    console.error('getCodingAssignmentPublic: unexpected error', err);
    return null;
  }
}

/**
 * Instructor-facing fetch of a coding assignment (includes Private tests + files).
 *
 * Hits `GET /v1.0/courses/{programId}/content/{contentId}/coding-assignment/full`.
 * Backend gates via `HasProgramManagementAccessAsync`. Returns the typed content,
 * or `null` on any failure.
 */
export async function getCodingAssignmentFull(
  programId: string,
  contentId: string,
): Promise<CodingAssignmentContent | null> {
  try {
    const client = getApiClient();
    const result = await client.request<unknown>({
      method: 'GET',
      path: `/v1.0/courses/${programId}/content/${contentId}/coding-assignment/full`,
      requiresAuth: true,
    });
    if (!result.ok) return null;
    return narrowCodingAssignmentContent(result.data);
  } catch (err) {
    console.error('getCodingAssignmentFull: unexpected error', err);
    return null;
  }
}

/**
 * Author or replace a coding assignment.
 *
 * Hits `PUT /v1.0/courses/{programId}/content/{contentId}/coding-assignment`.
 * Backend re-runs the FluentValidation rules server-side and returns
 * `Error.Validation(code, message)` on rejection.
 *
 * Returns `{success: true}` on HTTP 200, `{success: false, error}` on any
 * failure — `error` is the validation message (or fetch error) for display.
 */
export async function putCodingAssignment(
  programId: string,
  contentId: string,
  content: CodingAssignmentContent,
): Promise<PutCodingAssignmentResult> {
  try {
    const client = getApiClient();
    const result = await client.request<unknown>({
      method: 'PUT',
      path: `/v1.0/courses/${programId}/content/${contentId}/coding-assignment`,
      body: content,
      requiresAuth: true,
    });
    if (!result.ok) {
      return { success: false, error: extractError(result.error) };
    }
    return { success: true };
  } catch (err) {
    return {
      success: false,
      error: `Unexpected error: ${err instanceof Error ? err.message : String(err)}`,
    };
  }
}
