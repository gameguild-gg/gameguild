import { createServerClient, GeneratedApi, type ApiError } from '@game-guild/client';

import { getRequestAuthContext } from '@/auth';

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

/**
 * Authenticated authoring client. Server-side only: resolves the session token
 * per request so the authoring module's `requiresAuth` requests carry Bearer +
 * tenant headers, mirroring the actions.ts fetch header contract.
 */
async function createAuthoringBlogModules() {
  const { token, tenantId } = await getRequestAuthContext();
  if (!token || !tenantId) {
    throw new Error('You must be signed in to manage blog posts.');
  }

  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: async () => token },
    tenant: { getTenantId: async () => tenantId },
    timeout: Number.isFinite(PUBLIC_BLOG_API_TIMEOUT_MS) ? PUBLIC_BLOG_API_TIMEOUT_MS : 10_000,
  });

  return {
    authoringApi: new GeneratedApi.SocialBlogAuthoringModule(client),
    profilesApi: new GeneratedApi.SocialProfilesModule(client),
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

/**
 * Authoring-entity shape: same fields as the public detail DTO plus authorship
 * (primaryAuthorId) — the editor needs the id for primary-vs-coauthor role
 * checks, which the public DTO deliberately omits.
 */
export interface BlogPostAuthorView {
  id: string;
  primaryAuthorId: string;
  title: string | null;
  slug: string | null;
  excerpt: string | null;
  content: string | null;
  jsonBody: string | null;
  format: 'Markdown' | 'Lexical';
  tags: string[] | null;
  metaTitle: string | null;
  metaDescription: string | null;
  ogImageUrl: string | null;
  canonicalUrlOverride: string | null;
  twitterCard: string | null;
  structuredDataOverride: string | null;
  allowComments: boolean;
  status: 'Draft' | 'Published';
  publishedAt: string | null;
  revision: number;
  updatedAt: string;
}

export type MyPostLookup =
  | { status: 'ok'; post: BlogPostAuthorView }
  | { status: 'not-found' };

/**
 * Resolve an authoring route `(username, slug)` to the acting user's post.
 * The authoring surface is id-addressed (`GET mine` + `GET {id}`), so the slug
 * is matched within the actor's own post list — slugs are unique per primary
 * author, which the handle in the URL denotes.
 */
export async function getMyPostBySlug(username: string, slug: string): Promise<MyPostLookup> {
  void username;
  let authoringApi: InstanceType<typeof GeneratedApi.SocialBlogAuthoringModule>;
  try {
    ({ authoringApi } = await createAuthoringBlogModules());
  } catch {
    return { status: 'not-found' };
  }

  const mine = await authoringApi.getApiSocialBlogPostsMine();
  if (!mine.ok) {
    if (errorStatus(mine.error) === 404) return { status: 'not-found' };
    throw new Error(`Failed to load your blog posts: ${mine.error?.message ?? 'unknown error'}`);
  }

  const match = (mine.data ?? []).find((post) => post.slug === slug);
  if (!match?.id) return { status: 'not-found' };

  const detail = await authoringApi.getApiSocialBlogPosts(match.id);
  if (!detail.ok) {
    if (errorStatus(detail.error) === 404) return { status: 'not-found' };
    throw new Error(`Failed to load blog post: ${detail.error?.message ?? 'unknown error'}`);
  }

  return { status: 'ok', post: detail.data as unknown as BlogPostAuthorView };
}

/** Hub row for the workspace "Recent blog posts" card. */
export interface MyBlogPostRow {
  id: string;
  slug: string;
  title: string;
  status: 'Draft' | 'Published';
  format: 'Markdown' | 'Lexical';
  publishedAt: string | null;
  updatedAt: string;
  primaryAuthorHandle: string | null;
}

/**
 * The acting user's posts for hub management. The handle is resolved with a
 * read-only profile fetch (no or-create mutation on a listing surface); null
 * means the row links to the blogs index instead of the editor.
 */
export async function listMyBlogPosts(): Promise<MyBlogPostRow[]> {
  let authoringApi: InstanceType<typeof GeneratedApi.SocialBlogAuthoringModule>;
  let profilesApi: InstanceType<typeof GeneratedApi.SocialProfilesModule>;
  try {
    ({ authoringApi, profilesApi } = await createAuthoringBlogModules());
  } catch {
    return [];
  }

  const mine = await authoringApi.getApiSocialBlogPostsMine();
  if (!mine.ok) {
    throw new Error(`Failed to load your blog posts: ${mine.error?.message ?? 'unknown error'}`);
  }

  const posts = mine.data ?? [];
  if (posts.length === 0) return [];

  const primaryAuthorId = posts[0]?.primaryAuthorId;
  let handle: string | null = null;
  if (primaryAuthorId) {
    const profile = await profilesApi.getApiSocialProfilesUsers(primaryAuthorId);
    if (profile.ok && typeof profile.data.handle === 'string' && profile.data.handle) {
      handle = profile.data.handle;
    }
  }

  return posts.map((post) => ({
    id: post.id ?? '',
    slug: post.slug ?? '',
    title: post.title ?? 'Untitled',
    status: (post.status ?? 'Draft') as 'Draft' | 'Published',
    format: (post.format ?? 'Markdown') as 'Markdown' | 'Lexical',
    publishedAt: post.publishedAt ?? null,
    updatedAt: post.updatedAt,
    primaryAuthorHandle: handle,
  }));
}
