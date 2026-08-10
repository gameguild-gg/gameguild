/**
 * Server fetcher for the instructor /coding-definition/full endpoint.
 *
 * Unlike getCodingDefinitionPublic (enrollment-gated, hidden cases stripped),
 * this route is gated by CanReviewCourseAsync (Task 7) and returns the
 * UNREDACTED CodingDefinition — including hidden test cases. Returns null
 * when the assessment has no v2 coding definition or the caller lacks Review
 * permission (the API responds 403 in that case).
 */

import { getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';
import { cache } from 'react';
import type { CodingDefinition } from '@/lib/learning/queries/assessments';

function getApiClient() {
  const apiUrl =
    process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

/**
 * Fetch the FULL coding definition for an assessment (instructor-only,
 * includes hidden test cases). Returns null when absent or unauthorized.
 */
export const getCodingDefinitionFull = cache(
  async (assessmentId: string): Promise<CodingDefinition | null> => {
    try {
      const client = getApiClient();
      const result = await client.request<CodingDefinition>({
        method: 'GET',
        path: `/v1.0/assessments/${assessmentId}/coding-definition/full`,
      });
      if (!result.ok) return null;
      return result.data;
    } catch (err) {
      console.error('Error fetching full coding definition:', err);
      return null;
    }
  },
);
