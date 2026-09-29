'use server';

import { getRequestAuthContext } from '@/auth';

import type {
  AddBlogCommentInput,
  BlogComment,
  BlogPostDetail,
  CreateBlogPostInput,
  UpdateBlogPostDraftInput,
} from './types';

const apiBaseUrl = (
  process.env.API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  'http://localhost:8080'
).replace(/\/$/, '');

export type BlogActionResult<T> =
  | { success: true; data: T }
  | { success: false; error: string; status: number; code?: string }
  | {
      success: false;
      error: string;
      status: 409;
      code: 'revision-conflict';
      expectedRevision: number;
      currentRevision: number;
    };

interface ProblemDetails {
  title?: unknown;
  detail?: unknown;
  currentRevision?: unknown;
  expectedRevision?: unknown;
  errors?: unknown;
}

function readProblemField(body: unknown, name: string): unknown {
  if (body && typeof body === 'object' && name in body) {
    return (body as Record<string, unknown>)[name];
  }
  return undefined;
}

/**
 * Maps a failed API response to a typed action result. 409 ProblemDetails
 * responses carry `expectedRevision`/`currentRevision` and surface as the
 * `revision-conflict` variant so the editor can offer a reload.
 */
async function blogRequest<T>(path: string, init?: RequestInit): Promise<BlogActionResult<T>> {
  const { token, tenantId } = await getRequestAuthContext();
  if (!token || !tenantId) {
    return {
      success: false,
      error: 'You must be signed in to manage blog posts.',
      status: 401,
    };
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      'X-Tenant-Id': tenantId,
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
    cache: 'no-store',
  });

  const body = (await response.json().catch(() => null)) as ProblemDetails | T | null;
  if (!response.ok) {
    const error = readProblemField(body, 'detail');
    const message =
      (typeof error === 'string' && error) ||
      (typeof readProblemField(body, 'title') === 'string' && (readProblemField(body, 'title') as string)) ||
      response.statusText ||
      'Blog request failed.';

    if (response.status === 409) {
      return {
        success: false,
        error: message,
        status: 409,
        code: 'revision-conflict',
        expectedRevision: Number(readProblemField(body, 'expectedRevision') ?? 0),
        currentRevision: Number(readProblemField(body, 'currentRevision') ?? 0),
      };
    }

    return {
      success: false,
      error: message,
      status: response.status,
      code: typeof readProblemField(body, 'code') === 'string' ? (readProblemField(body, 'code') as string) : undefined,
    };
  }

  return { success: true, data: body as T };
}

function path(postId: string, suffix = ''): string {
  return `/api/social/blog/posts/${encodeURIComponent(postId)}${suffix}`;
}

export async function createPost(input: CreateBlogPostInput) {
  return blogRequest<BlogPostDetail>('/api/social/blog/posts', {
    method: 'POST',
    body: JSON.stringify(input),
  });
}

export async function updateDraft(postId: string, input: UpdateBlogPostDraftInput) {
  return blogRequest<BlogPostDetail>(path(postId), {
    method: 'PUT',
    body: JSON.stringify(input),
  });
}

export async function changeSlug(postId: string, newSlug: string) {
  return blogRequest<BlogPostDetail>(path(postId, '/slug'), {
    method: 'POST',
    body: JSON.stringify({ newSlug }),
  });
}

export async function coauthorAdd(postId: string, userId: string) {
  return blogRequest<void>(path(postId, '/coauthors'), {
    method: 'POST',
    body: JSON.stringify({ userId }),
  });
}

export async function coauthorRemove(postId: string, userId: string) {
  return blogRequest<void>(path(postId, `/coauthors/${encodeURIComponent(userId)}`), {
    method: 'DELETE',
  });
}

export async function transferPrimary(postId: string, newPrimaryUserId: string) {
  return blogRequest<BlogPostDetail>(path(postId, '/transfer-primary'), {
    method: 'POST',
    body: JSON.stringify({ newPrimaryUserId }),
  });
}

export async function publish(postId: string) {
  return blogRequest<BlogPostDetail>(path(postId, '/publish'), { method: 'POST' });
}

export async function unpublish(postId: string) {
  return blogRequest<BlogPostDetail>(path(postId, '/unpublish'), { method: 'POST' });
}

export async function deletePost(postId: string) {
  return blogRequest<void>(path(postId), { method: 'DELETE' });
}

export async function addComment(postId: string, input: AddBlogCommentInput) {
  return blogRequest<BlogComment>(path(postId, '/comments'), {
    method: 'POST',
    body: JSON.stringify(input),
  });
}

export async function deleteComment(commentId: string) {
  return blogRequest<void>(`/api/social/blog/comments/${encodeURIComponent(commentId)}`, {
    method: 'DELETE',
  });
}

export interface CreateAiRunInput {
  conversationId?: string | null;
  postRevision: number;
  instruction: string;
  proposalKind: string;
  selection?: string | null;
  idempotencyKey?: string | null;
}

export async function createAiRun(postId: string, input: CreateAiRunInput) {
  return blogRequest<Record<string, unknown>>(path(postId, '/ai/runs'), {
    method: 'POST',
    body: JSON.stringify(input),
  });
}

export async function cancelAiRun(postId: string, runId: string) {
  return blogRequest<Record<string, unknown>>(path(postId, `/ai/runs/${encodeURIComponent(runId)}/cancel`), {
    method: 'POST',
  });
}

export async function applyProposal(postId: string, proposalId: string, postRevision: number, cursorOffset?: number) {
  return blogRequest<Record<string, unknown>>(path(postId, `/ai/proposals/${encodeURIComponent(proposalId)}/apply`), {
    method: 'POST',
    body: JSON.stringify({ postRevision, cursorOffset }),
  });
}

export async function discardProposal(postId: string, proposalId: string) {
  return blogRequest<Record<string, unknown>>(path(postId, `/ai/proposals/${encodeURIComponent(proposalId)}`), {
    method: 'DELETE',
  });
}

// Re-export so callers (SSE proxy route in todo 10) can build raw requests
// against the same base URL without re-deriving env handling.
export { apiBaseUrl as BLOG_API_BASE_URL };
