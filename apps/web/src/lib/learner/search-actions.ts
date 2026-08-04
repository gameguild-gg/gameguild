'use server';

import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type LearningWorkspacesLearnerSearchResult,
} from '@game-guild/client';

export interface LearnerSearchItem {
  id: string;
  kind: string;
  title: string;
  description: string;
  route: string;
}

export type LearnerSearchActionResult =
  | { success: true; items: LearnerSearchItem[] }
  | { success: false; error: string };

function mapSearchItem(item: LearningWorkspacesLearnerSearchResult): LearnerSearchItem | null {
  if (!item.id || !item.title || !item.route || !item.route.startsWith('/') || item.route.startsWith('//')) {
    return null;
  }

  return {
    id: item.id,
    kind: item.kind || 'Learning resource',
    title: item.title,
    description: item.description || '',
    route: item.route,
  };
}

export async function searchLearnerWorkspace(query: string): Promise<LearnerSearchActionResult> {
  const normalizedQuery = query.trim();
  if (normalizedQuery.length < 2) {
    return { success: true, items: [] };
  }

  try {
    const client = createServerClient({
      baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080',
      auth: { getAccessToken: () => getToken() },
    });
    const result = await new GeneratedApi.LearningWorkspacesLearnerworkspaceModule(
      client,
    ).getLearningMeSearch({ q: normalizedQuery, take: 12 });

    if (!result.ok) {
      const error = result.error as { detail?: string; message?: string };
      return {
        success: false,
        error: error.detail || error.message || 'Learning search is temporarily unavailable.',
      };
    }

    return {
      success: true,
      items: result.data
        .map(mapSearchItem)
        .filter((item): item is LearnerSearchItem => item !== null),
    };
  } catch (error) {
    return {
      success: false,
      error:
        error instanceof Error && error.message
          ? error.message
          : 'Learning search is temporarily unavailable.',
    };
  }
}
