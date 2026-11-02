/**
 * Blog administration server surface (issue #96): platform-wide moderation reads and
 * mutations plus the Ghost content importer, backed by the SystemAdmin-gated
 * `api/social/blog/admin/*` endpoints. Fetch-based like `./actions` (same Bearer +
 * tenant header contract) so the admin console does not depend on generated-client
 * regeneration.
 */

import { getRequestAuthContext } from '@/auth';
import { assertSafeServiceUrl } from '@/lib/security/safe-remote-url';

const apiBaseUrl = (
  process.env.API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  'http://localhost:8080'
).replace(/\/$/, '');

export interface BlogAdminPost {
  id: string;
  primaryAuthorHandle: string;
  title: string;
  slug: string;
  status: 'Draft' | 'Published';
  publishedAt: string | null;
  updatedAt: string;
  commentsCount: number;
  viewsCount: number;
  allowComments: boolean;
  tags: string[] | null;
}

export interface BlogAdminComment {
  id: string;
  blogPostId: string;
  postTitle: string;
  postHandle: string;
  postSlug: string;
  parentCommentId: string | null;
  authorHandle: string;
  content: string;
  createdAt: string;
}

export interface BlogAdminStats {
  publishedPosts: number;
  draftPosts: number;
  commentsLast30Days: number;
}

export interface GhostImportOutcome {
  title: string;
  slug: string;
  authorHandle: string;
  result: 'Imported' | 'SkippedDuplicate' | 'SkippedNoAuthor' | 'SkippedNonPost';
  note?: string | null;
}

export interface GhostImportReport {
  dryRun: boolean;
  postsInExport: number;
  imported: number;
  skippedDuplicate: number;
  skippedNoAuthor: number;
  skippedNonPost: number;
  commentsInExport: number;
  outcomes: GhostImportOutcome[] | null;
}

export type AdminFetchOutcome<T> =
  | { status: 'ok'; data: T }
  | { status: 'forbidden' }
  | { status: 'error'; message: string };

async function adminFetch<T>(path: string, init?: RequestInit): Promise<AdminFetchOutcome<T>> {
  const { token, tenantId } = await getRequestAuthContext();
  if (!token || !tenantId) {
    return { status: 'forbidden' };
  }

  const response = await fetch(assertSafeServiceUrl(`${apiBaseUrl}${path}`, apiBaseUrl), {
    ...init,
    redirect: 'error',
    headers: {
      Authorization: `Bearer ${token}`,
      'X-Tenant-Id': tenantId,
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
    cache: 'no-store',
  });

  if (response.status === 403 || response.status === 401) {
    return { status: 'forbidden' };
  }

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { detail?: unknown; title?: unknown } | null;
    const message =
      (typeof body?.detail === 'string' && body.detail) ||
      (typeof body?.title === 'string' && body.title) ||
      response.statusText ||
      'Blog admin request failed.';
    return { status: 'error', message };
  }

  return { status: 'ok', data: (await response.json().catch(() => null)) as T };
}

function pageOrEmpty<T>(payload: { items?: T[] | null; hasMore?: boolean } | null) {
  return {
    items: payload?.items ?? [],
    hasMore: payload?.hasMore ?? false,
  };
}

export async function getBlogAdminPosts(page = 0, status?: 'Draft' | 'Published') {
  const search = new URLSearchParams({ page: String(page) });
  if (status) search.set('status', status);
  const outcome = await adminFetch<{ items?: BlogAdminPost[] | null; hasMore?: boolean } | null>(
    `/api/social/blog/admin/posts?${search.toString()}`,
  );
  if (outcome.status !== 'ok') return outcome;
  return { status: 'ok' as const, data: pageOrEmpty<BlogAdminPost>(outcome.data) };
}

export async function getBlogAdminComments(page = 0) {
  const outcome = await adminFetch<{ items?: BlogAdminComment[] | null; hasMore?: boolean } | null>(
    `/api/social/blog/admin/comments?page=${page}`,
  );
  if (outcome.status !== 'ok') return outcome;
  return { status: 'ok' as const, data: pageOrEmpty<BlogAdminComment>(outcome.data) };
}

export async function getBlogAdminStats() {
  return adminFetch<BlogAdminStats>('/api/social/blog/admin/stats');
}

export type AdminActionResult =
  | { success: true }
  | { success: false; error: string; status: number };

export async function adminUnpublishPost(postId: string): Promise<AdminActionResult> {
  const outcome = await adminFetch(`/api/social/blog/admin/posts/${encodeURIComponent(postId)}/unpublish`, {
    method: 'POST',
  });
  return outcome.status === 'ok'
    ? { success: true }
    : { success: false, error: outcome.status === 'forbidden' ? 'SystemAdmin is required.' : outcome.message, status: 403 };
}

export async function adminDeletePost(postId: string): Promise<AdminActionResult> {
  const outcome = await adminFetch(`/api/social/blog/admin/posts/${encodeURIComponent(postId)}`, {
    method: 'DELETE',
  });
  return outcome.status === 'ok'
    ? { success: true }
    : { success: false, error: outcome.status === 'forbidden' ? 'SystemAdmin is required.' : outcome.message, status: 403 };
}

export async function adminDeleteComment(commentId: string): Promise<AdminActionResult> {
  const outcome = await adminFetch(`/api/social/blog/admin/comments/${encodeURIComponent(commentId)}`, {
    method: 'DELETE',
  });
  return outcome.status === 'ok'
    ? { success: true }
    : { success: false, error: outcome.status === 'forbidden' ? 'SystemAdmin is required.' : outcome.message, status: 403 };
}

export async function adminImportGhost(
  exportJson: string,
  options: { dryRun: boolean; publishImported: boolean },
): Promise<{ success: true; report: GhostImportReport } | { success: false; error: string }> {
  const outcome = await adminFetch<GhostImportReport>('/api/social/blog/admin/import/ghost', {
    method: 'POST',
    body: JSON.stringify({
      exportJson,
      dryRun: options.dryRun,
      publishImported: options.publishImported,
    }),
  });

  if (outcome.status === 'ok') {
    return { success: true, report: outcome.data };
  }

  return {
    success: false,
    error: outcome.status === 'forbidden' ? 'SystemAdmin is required.' : outcome.message,
  };
}
