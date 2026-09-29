import { createServerClient, GeneratedApi, type ApiError } from '@game-guild/client';

import type {
  BlogComment,
  BlogCommentPage,
  BlogPostDetail,
  BlogPostSummary,
  BlogPostSummaryPage,
  BlogRouteResolution,
} from './types';

const DEFAULT_API_URL = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
const PUBLIC_BLOG_API_TIMEOUT_MS = Number(process.env.PUBLIC_BLOG_API_TIMEOUT_MS ?? 10_000);

function getApiUrl(): string {
  return DEFAULT_API_URL.replace(/\/$/, '');
}

function createPublicApiClient() {
  return createServerClient({
    baseUrl: getApiUrl(),
    timeout: Number.isFinite(PUBLIC_BLOG_API_TIMEOUT_MS) ? PUBLIC_BLOG_API_TIMEOUT_MS : 10_000,
  });
}

function createPublicBlogModules() {
  const client = createPublicApiClient();

  return {
    publicApi: new GeneratedApi.SocialBlogPublicModule(client),
  };
}

function errorStatus(error: ApiError | undefined): number | undefined {
  return error && typeof error.status === 'number' ? error.status : undefined;
}

/** Detail-or-redirect outcome for a `(handle, slug)` route. */
export type BlogPostLookup =
  | { status: 'ok'; post: BlogPostDetail }
  | { status: 'redirect'; redirect: BlogRouteResolution }
  | { status: 'not-found' };

export async function getBlogIndex(cursor?: {
  beforePublishedAt?: string;
  beforeId?: string;
}): Promise<BlogPostSummaryPage | null> {
  const { publicApi } = createPublicBlogModules();
  const result = await publicApi.getApiSocialBlogPublicPosts(cursor);
  if (!result.ok) {
    if (errorStatus(result.error) === 404) return null;
    throw new Error(`Failed to load blog index: ${result.error?.message ?? 'unknown error'}`);
  }

  return {
    items: (result.data.items ?? []) as BlogPostSummary[],
    hasMore: result.data.hasMore ?? false,
  };
}

export async function getAuthorPosts(
  handle: string,
  cursor?: { beforePublishedAt?: string; beforeId?: string },
): Promise<BlogPostSummaryPage | null> {
  const { publicApi } = createPublicBlogModules();
  const result = await publicApi.getApiSocialBlogPublicAuthorsForGetApiSocialBlogPublicAuthorsByHandle(handle, cursor);
  if (!result.ok) {
    if (errorStatus(result.error) === 404) return null;
    throw new Error(`Failed to load author posts: ${result.error?.message ?? 'unknown error'}`);
  }

  return {
    items: (result.data.items ?? []) as BlogPostSummary[],
    hasMore: result.data.hasMore ?? false,
  };
}

/**
 * Fetch a published post by route. Stale `(handle, slug)` pairs are resolved
 * through the slug-history endpoint so the page can 308-redirect to the
 * canonical route; unresolved routes yield `not-found` (caller calls
 * Next's `notFound()`).
 */
export async function getBlogPost(handle: string, slug: string): Promise<BlogPostLookup> {
  const { publicApi } = createPublicBlogModules();

  const detail = await publicApi.getApiSocialBlogPublicAuthorsForGetApiSocialBlogPublicAuthorsByHandleBySlug(handle, slug);
  if (detail.ok) {
    return { status: 'ok', post: detail.data as BlogPostDetail };
  }

  if (errorStatus(detail.error) !== 404) {
    throw new Error(`Failed to load blog post: ${detail.error?.message ?? 'unknown error'}`);
  }

  const resolution = await publicApi.getApiSocialBlogPublicResolve(handle, slug);
  if (resolution.ok && resolution.data.handle && resolution.data.slug) {
    return { status: 'redirect', redirect: { handle: resolution.data.handle, slug: resolution.data.slug } };
  }

  if (resolution.ok) {
    return { status: 'not-found' };
  }

  if (errorStatus(resolution.error) === 404) {
    return { status: 'not-found' };
  }

  throw new Error(`Failed to resolve blog route: ${resolution.error?.message ?? 'unknown error'}`);
}

export async function getBlogPostComments(
  postId: string,
  cursor?: { afterCreatedAt?: string; afterId?: string },
): Promise<BlogCommentPage | null> {
  const { publicApi } = createPublicBlogModules();
  const result = await publicApi.getApiSocialBlogPublicPostsComments(postId, cursor);
  if (!result.ok) {
    if (errorStatus(result.error) === 404) return null;
    throw new Error(`Failed to load comments: ${result.error?.message ?? 'unknown error'}`);
  }

  return {
    items: (result.data.items ?? []) as BlogComment[],
    hasMore: result.data.hasMore ?? false,
  };
}

export type { BlogPostSummary } from './types';
