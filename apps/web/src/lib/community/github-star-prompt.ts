'use server';

import { getToken } from '@/auth';
import { createServerClient } from '@game-guild/client';

/** Public repository metadata endpoint used for the star check (issue #90). */
const REPO_API_URL = 'https://api.github.com/repos/gameguild-gg/gameguild';

/**
 * Repository star count for the star prompt (issue #106 phase 1): fetched on
 * the server with a one-hour revalidation window so the browser no longer
 * calls api.github.com from a mount effect. Any failure renders the prompt
 * without the count.
 */
export async function fetchRepositoryStarCount(): Promise<number | null> {
  try {
    const response = await fetch(REPO_API_URL, {
      headers: { Accept: 'application/vnd.github+json' },
      next: { revalidate: 3600 },
    });
    if (!response.ok) {
      return null;
    }
    const repo = (await response.json()) as Partial<{ stargazers_count?: unknown }>;
    return typeof repo.stargazers_count === 'number' ? repo.stargazers_count : null;
  } catch {
    return null;
  }
}

/**
 * Server-side data source for the GitHub star prompt (issue #90).
 *
 * Detects whether the signed-in account is linked to GitHub via the metadata-only
 * HEAD /v1/auth/external-logins endpoint (X-Linked-Providers header). This covers
 * both "signed in with GitHub" and "linked GitHub afterwards".
 */
export async function isGitHubLinkedAction(): Promise<boolean> {
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });

  try {
    const result = await client.requestRaw<void>({
      method: 'HEAD',
      path: '/v1/auth/external-logins',
      requiresAuth: true,
    });
    if (!result.ok) {
      return false;
    }

    const header = result.data.headers.get('x-linked-providers') ?? '';
    return header
      .split(',')
      .map((pair) => pair.split('=')[0]?.trim().toLowerCase())
      .includes('github');
  } catch {
    // Fail closed: an unreachable API must not nag the user.
    return false;
  }
}
