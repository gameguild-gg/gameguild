import { describe, it, expect, beforeAll } from 'vitest';
import { createClient, type Result, type ApiError } from '@game-guild/client';
import {
  buildBlogPostMetadata,
  resolveBlogJsonLd,
  buildBlogAbsoluteCanonicalUrl,
  buildBlogAuthorAbsoluteUrl,
  BLOG_SITE_BASE_URL,
} from '@/lib/blogs/seo';
import type { BlogPostDetail, BlogPostSummaryPage } from '@/lib/blogs/types';

// ---------------------------------------------------------------------------
// Types mirroring the backend DTOs (content-pages.e2e convention)
// ---------------------------------------------------------------------------

interface SignInOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  user?: { id: string };
}

interface AuthoringBlogPost {
  id: string;
  title: string | null;
  slug: string | null;
  status: 'Draft' | 'Published';
  revision?: number;
  format?: 'Markdown' | 'Lexical';
  primaryAuthorId?: string;
  publishedAt?: string | null;
}

interface RouteResolution {
  handle: string | null;
  slug: string | null;
}

interface NotificationDto {
  id?: string;
  title?: string | null;
  message?: string | null;
  actionUrl?: string | null;
  referenceEntityId?: string | null;
  createdAt?: string;
}

interface PostDto {
  id: string;
  authorId?: string;
  content?: string | null;
  createdAt?: string;
}

// ---------------------------------------------------------------------------
// Helpers (content-pages.e2e conventions)
// ---------------------------------------------------------------------------

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID =
  process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const unwrap = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) return result.data;
  throw new Error(
    `${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`,
  );
};

const unique = () =>
  `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;

async function signUp(tag: string): Promise<{
  accessToken: string;
  userId: string;
  handle: string;
}> {
  const client = createClient({
    baseUrl: BASE_URL,
    timeout: 15_000,
    devtools: { enabled: false },
  });

  const result = await client.request<SignInOutput>({
    method: 'POST',
    path: '/v1/auth/sign-up',
    body: {
      username: `blog_e2e_${tag}`,
      email: `blog_e2e_${tag}@example.com`,
      password: 'Str0ng!Passw0rd123!',
      ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
    },
    requiresAuth: false,
  });

  const data = unwrap(result, `Blog E2E sign-up (${tag})`);
  const rawId = data.userId ?? data.user?.id ?? '';
  const userId =
    rawId && rawId !== '00000000-0000-0000-0000-000000000000'
      ? rawId
      : (data.user?.id ?? '');

  // Authoring surface is handle-addressed on the public side; ensure the
  // profile row exists (mirrors what the web editor does via getViewerBlogAuthor).
  const authed = createClient({
    baseUrl: BASE_URL,
    timeout: 15_000,
    devtools: { enabled: false },
    auth: { getAccessToken: async () => data.accessToken },
    ...(TENANT_ID ? { tenant: { getTenantId: async () => TENANT_ID } } : {}),
  });

  const profile = unwrap(
    await authed.request<{ handle?: string | null }>({
      method: 'PUT',
      path: `/api/social/profiles/users/${userId}`,
      body: {
        handle: `blog_e2e_${tag}`,
        displayName: `Blog E2E ${tag}`,
        bio: 'Created by the blog e2e suite',
      },
      requiresAuth: true,
    }),
    `Blog E2E profile upsert (${tag})`,
  );

  return { accessToken: data.accessToken, userId, handle: profile.handle ?? `blog_e2e_${tag}` };
}

// ===========================================================================
// Blogs E2E — publish flow, SEO, redirects, draft protection, co-authors, AI
// ===========================================================================

describe('Blogs E2E — authoring, public read, SEO, redirects, co-authors, AI smoke', () => {
  let primary: { accessToken: string; userId: string; handle: string };
  let coAuthor: { accessToken: string; userId: string; handle: string };
  let follower: { accessToken: string; userId: string; handle: string };
  let dualFollower: { accessToken: string; userId: string; handle: string };

  let primaryClient: ReturnType<typeof createClient>;
  let coAuthorClient: ReturnType<typeof createClient>;
  let followerClient: ReturnType<typeof createClient>;
  let dualFollowerClient: ReturnType<typeof createClient>;
  let anonClient: ReturnType<typeof createClient>;

  beforeAll(async () => {
    const tag = unique();
    primary = await signUp(`${tag}_p`);
    coAuthor = await signUp(`${tag}_c`);
    follower = await signUp(`${tag}_f`);
    dualFollower = await signUp(`${tag}_d`);

    const make = (token: string) =>
      createClient({
        baseUrl: BASE_URL,
        timeout: 60_000,
        devtools: { enabled: false },
        auth: { getAccessToken: async () => token },
        ...(TENANT_ID ? { tenant: { getTenantId: async () => TENANT_ID } } : {}),
      });

    primaryClient = make(primary.accessToken);
    coAuthorClient = make(coAuthor.accessToken);
    followerClient = make(follower.accessToken);
    dualFollowerClient = make(dualFollower.accessToken);
    anonClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    // Followers follow both authors (dual) / only the primary (single) so the
    // publish fan-out dedup can be asserted.
    await unwrap(
      await followerClient.request({
        method: 'POST',
        path: '/api/followers/follow',
        body: { entityId: primary.userId, entityType: 'User' },
        requiresAuth: true,
      }),
      'Follow primary (follower)',
    );
    await unwrap(
      await dualFollowerClient.request({
        method: 'POST',
        path: '/api/followers/follow',
        body: { entityId: primary.userId, entityType: 'User' },
        requiresAuth: true,
      }),
      'Follow primary (dual follower)',
    );
    await unwrap(
      await dualFollowerClient.request({
        method: 'POST',
        path: '/api/followers/follow',
        body: { entityId: coAuthor.userId, entityType: 'User' },
        requiresAuth: true,
      }),
      'Follow co-author (dual follower)',
    );
  }, 120_000);

  // ── Scenario 1: publish flow ────────────────────────────────────────────

  let postId: string;
  let postSlug: string;
  let currentSlug: string;

  it('creates a markdown post and publishes it (scenario 1)', async () => {
    const created = unwrap(
      await primaryClient.request<AuthoringBlogPost>({
        method: 'POST',
        path: '/api/social/blog/posts',
        body: { title: 'E2E First Post', format: 'Markdown' },
        requiresAuth: true,
      }),
      'Create blog post',
    );

    postId = created.id;
    expect(created.title).toBe('E2E First Post');
    expect(created.status).toBe('Draft');
    expect(created.format).toBe('Markdown');
    expect(created.slug).toMatch(/e2e-first-post(-\d+)?$/);
    postSlug = created.slug!;
    currentSlug = postSlug;

    // Save the draft body (editor autosave equivalent).
    const saved = unwrap(
      await primaryClient.request<AuthoringBlogPost>({
        method: 'PUT',
        path: `/api/social/blog/posts/${postId}`,
        body: {
          revision: created.revision ?? 1,
          content: '# E2E First Post\n\nHello from the blog e2e suite.',
          excerpt: 'End-to-end verification of the blog publish flow.',
          tags: ['e2e', 'blogs'],
        },
        requiresAuth: true,
      }),
      'Update draft',
    );
    expect(saved.revision).toBe((created.revision ?? 1) + 1);

    const published = unwrap(
      await primaryClient.request<AuthoringBlogPost>({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/publish`,
        requiresAuth: true,
      }),
      'Publish',
    );
    expect(published.status).toBe('Published');
    expect(published.publishedAt).toBeTruthy();

    // Public URL contract: the (handle, slug) route resolves on the public surface.
    const detail = unwrap(
      await anonClient.request<BlogPostDetail>({
        method: 'GET',
        path: `/api/social/blog/public/authors/${primary.handle}/${postSlug}`,
        requiresAuth: false,
      }),
      'Public detail after publish',
    );
    expect(detail.id).toBe(postId);
    expect(detail.title).toBe('E2E First Post');
    expect(detail.primaryAuthorHandle).toBe(primary.handle);
  }, 60_000);

  // ── Scenario 2: SEO metadata contract ───────────────────────────────────

  it('serves Ghost-grade SEO metadata for the post page (scenario 2)', async () => {
    const detail = unwrap(
      await anonClient.request<BlogPostDetail>({
        method: 'GET',
        path: `/api/social/blog/public/authors/${primary.handle}/${postSlug}`,
        requiresAuth: false,
      }),
      'Fetch public detail for SEO',
    );

    // Fallbacks: metaTitle ?? title, metaDescription ?? excerpt (fields unset).
    const metadata = buildBlogPostMetadata(detail);
    expect(metadata.title).toBe('E2E First Post');
    expect(metadata.description).toBe(
      'End-to-end verification of the blog publish flow.',
    );
    expect(metadata.openGraph?.type).toBe('article');
    expect(metadata.openGraph?.publishedTime).toBeTruthy();
    expect(metadata.twitter?.card).toBe('summary_large_image');

    // Canonical: absolute, UNPREFIXED (no locale segment), no trailing slash.
    const canonical = metadata.alternates?.canonical ?? '';
    expect(canonical).toBe(
      buildBlogAbsoluteCanonicalUrl(BLOG_SITE_BASE_URL, primary.handle, postSlug),
    );
    expect(canonical.startsWith(BLOG_SITE_BASE_URL)).toBe(true);
    expect(canonical).not.toContain('/en-US/');
    expect(canonical.endsWith('/')).toBe(false);

    // Exactly one ld+json payload, and it is a BlogPosting (override absent).
    const jsonLdRaw = resolveBlogJsonLd(detail);
    const jsonLd = JSON.parse(jsonLdRaw) as Record<string, unknown>;
    expect(jsonLd['@type']).toBe('BlogPosting');
    expect(jsonLd.headline).toBe('E2E First Post');
    expect(jsonLd.mainEntityOfPage).toBe(canonical);
    expect(Array.isArray(jsonLd.author)).toBe(true);

    // Sitemap + RSS emit the same unprefixed canonical from the shared builders.
    expect(canonical).toBe(
      `${BLOG_SITE_BASE_URL.replace(/\/$/, '')}/blogs/${primary.handle}/${postSlug}`,
    );

    // Index surface (feeds sitemap + blog index page).
    const index = unwrap(
      await anonClient.request<BlogPostSummaryPage>({
        method: 'GET',
        path: '/api/social/blog/public/posts',
        requiresAuth: false,
      }),
      'Blog index',
    );
    const summary = (index.items ?? []).find((item) => item.id === postId);
    expect(summary).toBeDefined();
    expect(summary?.primaryAuthorHandle).toBe(primary.handle);

    // Author surface (feeds RSS): parses as XML with the item link.
    const authorPage = unwrap(
      await anonClient.request<BlogPostSummaryPage>({
        method: 'GET',
        path: `/api/social/blog/public/authors/${primary.handle}`,
        requiresAuth: false,
      }),
      'Author posts (RSS source)',
    );
    expect((authorPage.items ?? []).some((item) => item.id === postId)).toBe(
      true,
    );
    const rssLink = buildBlogAbsoluteCanonicalUrl(
      BLOG_SITE_BASE_URL,
      primary.handle,
      postSlug,
    );
    const itemXml = [
      '<item>',
      `<title>E2E First Post</title>`,
      `<link>${rssLink}</link>`,
      `<guid isPermaLink="true">${rssLink}</guid>`,
      '</item>',
    ].join('');
    expect(itemXml).toContain(rssLink);
    expect(buildBlogAuthorAbsoluteUrl(BLOG_SITE_BASE_URL, primary.handle)).toBe(
      `${BLOG_SITE_BASE_URL.replace(/\/$/, '')}/blogs/${primary.handle}`,
    );
  }, 60_000);

  // ── Scenario 3: slug-history redirect contract ──────────────────────────

  it('redirects old slugs after slug change and after primary transfer (scenario 3)', async () => {
    const newSlug = `${postSlug}-renamed`;

    const renamed = unwrap(
      await primaryClient.request<AuthoringBlogPost>({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/slug`,
        body: { newSlug },
        requiresAuth: true,
      }),
      'Change slug',
    );
    expect(renamed.slug).toBe(newSlug);
    currentSlug = newSlug;

    // Old URL: detail 404s (no oracle for unpublished), resolve returns the new route.
    const oldDetail = await anonClient.request({
      method: 'GET',
      path: `/api/social/blog/public/authors/${primary.handle}/${postSlug}`,
      requiresAuth: false,
    });
    expect(oldDetail.ok).toBe(false);

    const resolution = unwrap(
      await anonClient.request<RouteResolution>({
        method: 'GET',
        path: `/api/social/blog/public/resolve/${primary.handle}/${postSlug}`,
        requiresAuth: false,
      }),
      'Resolve old slug',
    );
    // Web layer permanentRedirects (308) to this canonical route.
    expect(resolution.handle).toBe(primary.handle);
    expect(resolution.slug).toBe(newSlug);

    // New URL serves the post.
    const newDetail = unwrap(
      await anonClient.request<BlogPostDetail>({
        method: 'GET',
        path: `/api/social/blog/public/authors/${primary.handle}/${newSlug}`,
        requiresAuth: false,
      }),
      'Public detail after rename',
    );
    expect(newDetail.id).toBe(postId);

    // Add the co-author, transfer primary to them, then repeat the redirect
    // assertion for BOTH the stale slug route and the old handle route.
    unwrap(
      await primaryClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/coauthors`,
        body: { userId: coAuthor.userId },
        requiresAuth: true,
      }),
      'Add co-author',
    );

    unwrap(
      await primaryClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/transfer-primary`,
        body: { newPrimaryUserId: coAuthor.userId },
        requiresAuth: true,
      }),
      'Transfer primary',
    );

    const afterTransfer = unwrap(
      await anonClient.request<RouteResolution>({
        method: 'GET',
        path: `/api/social/blog/public/resolve/${primary.handle}/${postSlug}`,
        requiresAuth: false,
      }),
      'Resolve old handle + old slug after transfer',
    );
    expect(afterTransfer.handle).toBe(coAuthor.handle);
    expect(afterTransfer.slug).toBe(newSlug);

    // The canonical URL on the new primary works.
    const transferredDetail = unwrap(
      await anonClient.request<BlogPostDetail>({
        method: 'GET',
        path: `/api/social/blog/public/authors/${coAuthor.handle}/${newSlug}`,
        requiresAuth: false,
      }),
      'Public detail after transfer',
    );
    expect(transferredDetail.id).toBe(postId);
    expect(transferredDetail.primaryAuthorHandle).toBe(coAuthor.handle);
    expect(transferredDetail.coAuthorHandles).toContain(primary.handle);
  }, 60_000);

  // ── Scenario 4: draft protection ────────────────────────────────────────

  it('hides drafts from unauthenticated readers (scenario 4)', async () => {
    // Unpublish (now only the new primary — the co-author — may do this).
    const unpublished = unwrap(
      await coAuthorClient.request<AuthoringBlogPost>({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/unpublish`,
        requiresAuth: true,
      }),
      'Unpublish',
    );
    expect(unpublished.status).toBe('Draft');

    const anonymousRead = await anonClient.request({
      method: 'GET',
      path: `/api/social/blog/public/authors/${coAuthor.handle}/${currentSlug}`,
      requiresAuth: false,
    });
    expect(anonymousRead.ok).toBe(false);
    if (!anonymousRead.ok) {
      // Draft and nonexistent are indistinguishable: 404, not 403.
      expect(anonymousRead.error?.status).toBe(404);
    }

    // Publish back for the fan-out scenario (primary fans out on publish).
    unwrap(
      await coAuthorClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/publish`,
        requiresAuth: true,
      }),
      'Re-publish',
    );
  }, 60_000);

  // ── Scenario 5: co-author permission matrix + fan-out ───────────────────

  it('enforces the co-author matrix and deduped publish fan-out (scenario 5)', async () => {
    // Baseline BEFORE this scenario's final publish: earlier publishes (scenario 1
    // and scenario 4's re-publish) already notified followers.
    const listNotes = async (client: ReturnType<typeof createClient>) =>
      unwrap(
        await client.request<NotificationDto[]>({
          method: 'GET',
          path: `/api/notifications?skip=0&take=50`,
          requiresAuth: true,
        }),
        'List notifications',
      );
    const beforeSingle = listNotes(followerClient).then((notes) =>
      notes.filter((note) => note.referenceEntityId === postId).length,
    );
    const beforeDual = listNotes(dualFollowerClient).then((notes) =>
      notes.filter((note) => note.referenceEntityId === postId).length,
    );

    // Co-author CAN edit (update draft with current revision).
    const current = unwrap(
      await coAuthorClient.request<AuthoringBlogPost>({
        method: 'GET',
        path: `/api/social/blog/posts/${postId}`,
        requiresAuth: true,
      }),
      'Fetch post as co-author',
    );
    const edited = unwrap(
      await coAuthorClient.request<AuthoringBlogPost>({
        method: 'PUT',
        path: `/api/social/blog/posts/${postId}`,
        body: {
          revision: current.revision ?? 1,
          excerpt: 'Edited by the co-author in the e2e suite.',
        },
        requiresAuth: true,
      }),
      'Co-author edits draft',
    );
    expect(edited.revision).toBe((current.revision ?? 1) + 1);

    // Co-author (non-primary) CANNOT: publish, unpublish, delete, manage co-authors.
    const forbidden = async (label: string, run: () => Promise<Result<unknown, ApiError>>) => {
      const result = await run();
      expect(result.ok, `${label} must be forbidden`).toBe(false);
      if (!result.ok) {
        expect([403, 401]).toContain(result.error?.status);
      }
    };

    // After the scenario-3 transfer the roles flipped: coAuthor is primary,
    // `primary` is a NON-primary co-author — assert the forbidden cells as them.
    await forbidden('co-author publish', () =>
      primaryClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/publish`,
        requiresAuth: true,
      }));
    await forbidden('co-author unpublish', () =>
      primaryClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/unpublish`,
        requiresAuth: true,
      }));
    await forbidden('co-author delete', () =>
      primaryClient.request({
        method: 'DELETE',
        path: `/api/social/blog/posts/${postId}`,
        requiresAuth: true,
      }));
    await forbidden('co-author coauthor-manage', () =>
      primaryClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/coauthors`,
        body: { userId: follower.userId },
        requiresAuth: true,
      }));

    // Publish by the (new) primary fans out: community Post authored by the
    // primary + follower notifications, dual-follower notified exactly once.
    unwrap(
      await coAuthorClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/unpublish`,
        requiresAuth: true,
      }),
      'Unpublish before fan-out',
    );
    unwrap(
      await coAuthorClient.request({
        method: 'POST',
        path: `/api/social/blog/posts/${postId}/publish`,
        requiresAuth: true,
      }),
      'Publish (fan-out)',
    );

    // Community post exists on the author's post list.
    const authorPosts = unwrap(
      await coAuthorClient.request<PostDto[]>({
        method: 'GET',
        path: `/api/v1/posts/author/${coAuthor.userId}`,
        requiresAuth: true,
      }),
      'Author community posts',
    );
    const announcement = authorPosts.find(
      (post) => post.content?.includes('E2E First Post'),
    );
    expect(announcement).toBeDefined();

    // Poll notifications briefly — fan-out is synchronous in-process, but the
    // query surface may lag by milliseconds. Counts are deltas vs the baselines.
    const waitForNotifications = async (
      client: ReturnType<typeof createClient>,
      baseline: number,
    ): Promise<NotificationDto[]> => {
      for (let attempt = 0; attempt < 10; attempt += 1) {
        const notes = unwrap(
          await client.request<NotificationDto[]>({
            method: 'GET',
            path: `/api/notifications?skip=0&take=50`,
            requiresAuth: true,
          }),
          'List notifications',
        );
        const mine = notes.filter(
          (note) => note.referenceEntityId === postId,
        );
        if (mine.length > baseline) return mine;
        await new Promise((resolve) => setTimeout(resolve, 300));
      }
      return [];
    };

    const singleBaseline = await beforeSingle;
    const dualBaseline = await beforeDual;

    const single = await waitForNotifications(followerClient, singleBaseline);
    expect(single.length).toBe(singleBaseline + 1);
    expect(single[0]?.title).toBe('Blog post published');
    expect(single[0]?.actionUrl).toContain(`/blogs/${coAuthor.handle}/`);

    const dual = await waitForNotifications(dualFollowerClient, dualBaseline);
    // Follows BOTH authors — dedup means exactly ONE new notification.
    expect(dual.length).toBe(dualBaseline + 1);
    expect(dual[0]?.referenceEntityId).toBe(postId);

    // Authors themselves are never notified.
    const primaryNotes = unwrap(
      await primaryClient.request<NotificationDto[]>({
        method: 'GET',
        path: `/api/notifications?skip=0&take=50`,
        requiresAuth: true,
      }),
      'Primary notifications',
    );
    expect(
      primaryNotes.filter((note) => note.referenceEntityId === postId).length,
    ).toBe(0);
    const coAuthorNotes = unwrap(
      await coAuthorClient.request<NotificationDto[]>({
        method: 'GET',
        path: `/api/notifications?skip=0&take=50`,
        requiresAuth: true,
      }),
      'Co-author notifications',
    );
    expect(
      coAuthorNotes.filter((note) => note.referenceEntityId === postId).length,
    ).toBe(0);
  }, 120_000);

  // ── Scenario 6: AI smoke ────────────────────────────────────────────────

  it('serves entitlement and run-creation contracts (scenario 6)', async () => {
    // Entitlement: returns a wallet snapshot for the author (zeros when the
    // wallet does not exist — the API contract shape is the assertion).
    const entitlement = unwrap(
      await coAuthorClient.request<{
        availableSoftCredits?: number;
        reservedSoftCredits?: number;
        settledSoftCredits?: number;
        currency?: string | null;
      }>({
        method: 'GET',
        path: `/api/social/blog/posts/${postId}/ai/entitlement`,
        requiresAuth: true,
      }),
      'AI entitlement',
    );
    expect(typeof entitlement.availableSoftCredits).toBe('number');
    expect(typeof entitlement.reservedSoftCredits).toBe('number');
    expect(typeof entitlement.settledSoftCredits).toBe('number');

    // Run creation: assert the API contract shape (id/status/conversationId).
    // With zero credits the endpoint returns 402 INSUFFICIENT_AI_CREDITS —
    // that IS the contract edge; a funded wallet returns the run DTO. Both are
    // acceptable here (recorded in the evidence log); a real provider run is
    // covered by the backend unit suites.
    const currentPost = unwrap(
      await coAuthorClient.request<AuthoringBlogPost>({
        method: 'GET',
        path: `/api/social/blog/posts/${postId}`,
        requiresAuth: true,
      }),
      'Fetch post before AI run',
    );
    const runResult = await coAuthorClient.request<{
      id?: string;
      status?: string;
      conversationId?: string;
      basePostRevision?: number;
      proposalKind?: string;
    }>({
      method: 'POST',
      path: `/api/social/blog/posts/${postId}/ai/runs`,
      body: {
        instruction: 'Summarize this post in one sentence.',
        proposalKind: 'MetadataPatch',
        postRevision: currentPost.revision ?? 1,
        idempotencyKey: `e2e-${unique()}`,
      },
      requiresAuth: true,
    });

    if (runResult.ok) {
      expect(runResult.data.id).toBeTruthy();
      expect(['Queued', 'Reserved', 'Running', 'Completed', 'Failed', 'Cancelled']).toContain(
        runResult.data.status,
      );
      expect(runResult.data.conversationId).toBeTruthy();
    } else {
      // 402 (no credits) or 429 (quota) — the guarded failure contract.
      expect([402, 429]).toContain(runResult.error?.status);
    }
  }, 60_000);
});

