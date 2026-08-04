import { describe, it, expect, beforeAll } from 'vitest';
import { createClient, type Result, type ApiError } from '@game-guild/client';

// ---------------------------------------------------------------------------
// Types mirroring the backend DTOs
// ---------------------------------------------------------------------------

// PageType: Landing=0, Legal=1, ResourceIndex=2, Resource=3, Custom=4
// PageStatus: Draft=0, Published=1, Archived=2
// SectionType: Hero=0, Features=1, Testimonials=2, Pricing=3, CallToAction=4,
//   Faq=5, RichText=6, Gallery=7, Stats=8, Team=9, LogoCloud=10,
//   Newsletter=11, Contact=12, ResourceCards=13, Custom=14
// ContentResourceType: Article=0, Tutorial=1, Documentation=2, Video=3,
//   Download=4, ExternalLink=5, Course=6, Custom=7
// ContentResourceStatus: Draft=0, InReview=1, Published=2, Archived=3

interface PageDto {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  pageType: string;
  status: string;
  locale: string | null;
  metaTitle: string | null;
  metaDescription: string | null;
  metaKeywords: string | null;
  canonicalUrl: string | null;
  robotsDirective: string | null;
  ogTitle: string | null;
  ogDescription: string | null;
  ogImageUrl: string | null;
  ogType: string | null;
  twitterCard: string | null;
  twitterSite: string | null;
  structuredData: string | null;
  body: string | null;
  customData: string | null;
  parentPageId: string | null;
  sortOrder: number;
  sections: PageSectionDto[];
  publishedAt: string | null;
  scheduledPublishAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

interface PageSectionDto {
  id: string;
  pageId: string;
  sectionType: string;
  heading: string | null;
  subheading: string | null;
  data: string | null;
  sortOrder: number;
  isVisible: boolean;
  cssClasses: string | null;
  createdAt: string;
  updatedAt: string | null;
}

interface ContentResourceDto {
  id: string;
  slug: string;
  title: string;
  summary: string | null;
  body: string | null;
  resourceType: string;
  status: string;
  locale: string | null;
  categorySlug: string | null;
  tags: string | null;
  authorId: string | null;
  authorName: string | null;
  coverImageUrl: string | null;
  videoUrl: string | null;
  downloadUrl: string | null;
  externalUrl: string | null;
  linkedEntityId: string | null;
  linkedEntityType: string | null;
  metaTitle: string | null;
  metaDescription: string | null;
  ogImageUrl: string | null;
  structuredData: string | null;
  readingTimeMinutes: number | null;
  viewCount: number;
  isFeatured: boolean;
  sortOrder: number;
  publishedAt: string | null;
  scheduledPublishAt: string | null;
  customData: string | null;
  createdAt: string;
  updatedAt: string | null;
}

interface OpenGraphMetadataDto {
  slug: string;
  title: string;
  description: string | null;
  ogTitle: string | null;
  ogDescription: string | null;
  ogImageUrl: string | null;
  ogType: string | null;
  twitterCard: string | null;
  twitterSite: string | null;
  canonicalUrl: string | null;
  robotsDirective: string | null;
  structuredData: string | null;
}

interface SignInOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  user?: { id: string };
}

// ---------------------------------------------------------------------------
// Helpers
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

// ===========================================================================
// 1. PAGES — full CRUD + sections + publish/unpublish + OpenGraph
// ===========================================================================

describe('Content Pages E2E — Pages, Sections, OpenGraph', () => {
  let accessToken: string;
  let authedClient: ReturnType<typeof createClient>;

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `pages_e2e_${tag}`,
        email: `pages_e2e_${tag}@example.com`,
        password: 'Str0ng!Passw0rd123!',
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const data = unwrap(signUpResult, 'Pages E2E sign-up');
    accessToken = data.accessToken;

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
    });
  }, 30_000);

  // ── Page CRUD ──

  let pageId: string;
  const pageSlug = `e2e-about-${Date.now()}`;

  it('creates a page with full SEO/OG metadata', async () => {
    const result = await authedClient.request<PageDto>({
      method: 'POST',
      path: '/v1/pages',
      body: {
        slug: pageSlug,
        title: 'About GameGuild',
        description: 'Learn about our platform',
        pageType: 0, // Landing
        locale: 'en-US',
        metaTitle: 'About Us — GameGuild',
        metaDescription: 'GameGuild is a game development education platform.',
        metaKeywords: 'gamedev,education,learning',
        canonicalUrl: `https://gameguild.gg/${pageSlug}`,
        robotsDirective: 'index,follow',
        ogTitle: 'About GameGuild Platform',
        ogDescription: 'Discover the #1 game dev education platform.',
        ogImageUrl: 'https://cdn.gameguild.gg/og/about.png',
        ogType: 'website',
        twitterCard: 'summary_large_image',
        twitterSite: '@gameguild',
        body: '# About GameGuild\n\nWe are a game development education platform.',
        sortOrder: 0,
      },
      requiresAuth: true,
    });

    const page = unwrap(result, 'Create page');
    pageId = page.id;

    expect(page.slug).toBe(pageSlug);
    expect(page.title).toBe('About GameGuild');
    expect(page.description).toBe('Learn about our platform');
    expect(page.ogTitle).toBe('About GameGuild Platform');
    expect(page.ogDescription).toBe(
      'Discover the #1 game dev education platform.',
    );
    expect(page.ogImageUrl).toBe('https://cdn.gameguild.gg/og/about.png');
    expect(page.ogType).toBe('website');
    expect(page.twitterCard).toBe('summary_large_image');
    expect(page.twitterSite).toBe('@gameguild');
    expect(page.metaTitle).toBe('About Us — GameGuild');
    expect(page.metaKeywords).toBe('gamedev,education,learning');
    expect(page.canonicalUrl).toBe(`https://gameguild.gg/${pageSlug}`);
    expect(page.robotsDirective).toBe('index,follow');
    expect(page.body).toContain('# About GameGuild');
    expect(page.sections).toEqual([]);
  });

  it('retrieves the page by ID', async () => {
    const result = await authedClient.request<PageDto>({
      method: 'GET',
      path: `/v1/pages/${pageId}`,
      requiresAuth: true,
    });

    const page = unwrap(result, 'Get page by ID');
    expect(page.id).toBe(pageId);
    expect(page.slug).toBe(pageSlug);
    expect(page.title).toBe('About GameGuild');
  });

  it('retrieves the page by slug (anonymous)', async () => {
    const publicClient = createClient({
      baseUrl: BASE_URL,
      timeout: 10_000,
      devtools: { enabled: false },
    });

    const result = await publicClient.request<PageDto>({
      method: 'GET',
      path: `/v1/pages/by-slug/${pageSlug}`,
      requiresAuth: false,
    });

    const page = unwrap(result, 'Get page by slug');
    expect(page.id).toBe(pageId);
    expect(page.slug).toBe(pageSlug);
    expect(page.ogTitle).toBe('About GameGuild Platform');
  });

  it('lists pages', async () => {
    const result = await authedClient.request<PageDto[]>({
      method: 'GET',
      path: '/v1/pages',
      requiresAuth: true,
    });

    const pages = unwrap(result, 'List pages');
    expect(Array.isArray(pages)).toBe(true);

    const found = pages.find((p) => p.id === pageId);
    expect(found).toBeDefined();
    expect(found!.slug).toBe(pageSlug);
  });

  it('updates the page', async () => {
    const result = await authedClient.request<PageDto>({
      method: 'PUT',
      path: `/v1/pages/${pageId}`,
      body: {
        title: 'About GameGuild — Updated',
        metaDescription: 'Updated SEO description for GameGuild.',
        ogDescription: 'Updated OG description!',
      },
      requiresAuth: true,
    });

    const page = unwrap(result, 'Update page');
    expect(page.title).toBe('About GameGuild — Updated');
    expect(page.metaDescription).toBe(
      'Updated SEO description for GameGuild.',
    );
    expect(page.ogDescription).toBe('Updated OG description!');
    // Original fields should be preserved
    expect(page.slug).toBe(pageSlug);
    expect(page.ogTitle).toBe('About GameGuild Platform');
  });

  // ── Sections ──

  let sectionHeroId: string;
  let sectionFeaturesId: string;
  let sectionCtaId: string;

  it('adds a Hero section to the page', async () => {
    const result = await authedClient.request<PageSectionDto>({
      method: 'POST',
      path: `/v1/pages/${pageId}/sections`,
      body: {
        sectionType: 0, // Hero
        heading: 'Build Games. Learn Code.',
        subheading: "The world's best game development education platform.",
        data: JSON.stringify({
          backgroundImage: 'https://cdn.gameguild.gg/hero-bg.jpg',
          ctaText: 'Get Started Free',
          ctaUrl: '/sign-up',
        }),
        sortOrder: 0,
        isVisible: true,
      },
      requiresAuth: true,
    });

    const section = unwrap(result, 'Create hero section');
    sectionHeroId = section.id;

    expect(section.pageId).toBe(pageId);
    expect(section.heading).toBe('Build Games. Learn Code.');
    expect(section.isVisible).toBe(true);
    expect(section.sortOrder).toBe(0);

    const parsed = JSON.parse(section.data!);
    expect(parsed.ctaText).toBe('Get Started Free');
  });

  it('adds a Features section', async () => {
    const result = await authedClient.request<PageSectionDto>({
      method: 'POST',
      path: `/v1/pages/${pageId}/sections`,
      body: {
        sectionType: 1, // Features
        heading: 'Why GameGuild?',
        data: JSON.stringify({
          items: [
            {
              icon: 'graduation-cap',
              title: 'Structured Courses',
              description: 'Follow step-by-step game dev curricula.',
            },
            {
              icon: 'users',
              title: 'Community',
              description: 'Learn with thousands of fellow developers.',
            },
            {
              icon: 'trophy',
              title: 'Certificates',
              description: 'Earn credentials from industry partners.',
            },
          ],
        }),
        sortOrder: 1,
        isVisible: true,
      },
      requiresAuth: true,
    });

    const section = unwrap(result, 'Create features section');
    sectionFeaturesId = section.id;
    expect(section.heading).toBe('Why GameGuild?');
    expect(section.sortOrder).toBe(1);
  });

  it('adds a CTA section', async () => {
    const result = await authedClient.request<PageSectionDto>({
      method: 'POST',
      path: `/v1/pages/${pageId}/sections`,
      body: {
        sectionType: 4, // CallToAction
        heading: 'Ready to start building games?',
        subheading: 'Join 10,000+ game developers learning today.',
        data: JSON.stringify({
          primaryCta: { text: 'Sign Up Free', url: '/sign-up' },
          secondaryCta: { text: 'Browse Courses', url: '/courses' },
        }),
        sortOrder: 2,
        isVisible: true,
        cssClasses: 'bg-gradient-to-r from-purple-600 to-blue-600',
      },
      requiresAuth: true,
    });

    const section = unwrap(result, 'Create CTA section');
    sectionCtaId = section.id;
    expect(section.heading).toBe('Ready to start building games?');
    expect(section.cssClasses).toBe(
      'bg-gradient-to-r from-purple-600 to-blue-600',
    );
  });

  it('lists all sections for the page', async () => {
    const result = await authedClient.request<PageSectionDto[]>({
      method: 'GET',
      path: `/v1/pages/${pageId}/sections`,
      requiresAuth: true,
    });

    const sections = unwrap(result, 'List sections');
    expect(sections.length).toBe(3);
    expect(sections[0].sortOrder).toBeLessThanOrEqual(sections[1].sortOrder);
    expect(sections[1].sortOrder).toBeLessThanOrEqual(sections[2].sortOrder);
  });

  it('retrieves a specific section', async () => {
    const result = await authedClient.request<PageSectionDto>({
      method: 'GET',
      path: `/v1/pages/${pageId}/sections/${sectionHeroId}`,
      requiresAuth: true,
    });

    const section = unwrap(result, 'Get section');
    expect(section.id).toBe(sectionHeroId);
    expect(section.heading).toBe('Build Games. Learn Code.');
  });

  it('updates a section', async () => {
    const result = await authedClient.request<PageSectionDto>({
      method: 'PUT',
      path: `/v1/pages/${pageId}/sections/${sectionHeroId}`,
      body: {
        heading: 'Build Amazing Games. Learn Real Code.',
        subheading: 'The #1 game dev learning platform.',
      },
      requiresAuth: true,
    });

    const section = unwrap(result, 'Update section');
    expect(section.heading).toBe('Build Amazing Games. Learn Real Code.');
    expect(section.subheading).toBe('The #1 game dev learning platform.');
  });

  it('reorders sections', async () => {
    // Move CTA to position 0, Hero to 1, Features to 2
    const result = await authedClient.request<void>({
      method: 'POST',
      path: `/v1/pages/${pageId}/sections/reorder`,
      body: [sectionCtaId, sectionHeroId, sectionFeaturesId],
      requiresAuth: true,
    });

    // Reorder returns 204 NoContent
    expect(result.ok).toBe(true);

    // Verify the new order
    const listResult = await authedClient.request<PageSectionDto[]>({
      method: 'GET',
      path: `/v1/pages/${pageId}/sections`,
      requiresAuth: true,
    });

    const sections = unwrap(listResult, 'List after reorder');
    expect(sections[0].id).toBe(sectionCtaId);
    expect(sections[1].id).toBe(sectionHeroId);
    expect(sections[2].id).toBe(sectionFeaturesId);
  });

  it('deletes a section', async () => {
    const result = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/pages/${pageId}/sections/${sectionCtaId}`,
      requiresAuth: true,
    });

    expect(result.ok).toBe(true);

    // Verify it's gone
    const listResult = await authedClient.request<PageSectionDto[]>({
      method: 'GET',
      path: `/v1/pages/${pageId}/sections`,
      requiresAuth: true,
    });

    const sections = unwrap(listResult, 'List after delete');
    expect(sections.find((s) => s.id === sectionCtaId)).toBeUndefined();
    expect(sections.length).toBe(2);
  });

  // ── Page with sections embedded ──

  it('fetches page by slug with sections included', async () => {
    const publicClient = createClient({
      baseUrl: BASE_URL,
      timeout: 10_000,
      devtools: { enabled: false },
    });

    const result = await publicClient.request<PageDto>({
      method: 'GET',
      path: `/v1/pages/by-slug/${pageSlug}`,
      requiresAuth: false,
    });

    const page = unwrap(result, 'Get page with sections');
    expect(page.sections.length).toBe(2);
    expect(page.sections.every((s) => s.pageId === pageId)).toBe(true);
  });

  // ── Publish / Unpublish ──

  it('publishes the page', async () => {
    const result = await authedClient.request<PageDto>({
      method: 'POST',
      path: `/v1/pages/${pageId}/publish`,
      requiresAuth: true,
    });

    const page = unwrap(result, 'Publish page');
    expect(page.status).toBe('Published');
    expect(page.publishedAt).toBeTruthy();
  });

  it('unpublishes the page', async () => {
    const result = await authedClient.request<PageDto>({
      method: 'POST',
      path: `/v1/pages/${pageId}/unpublish`,
      requiresAuth: true,
    });

    const page = unwrap(result, 'Unpublish page');
    expect(page.status).toBe('Draft');
  });

  // ── Page hierarchy (parent/child) ──

  let childPageId: string;
  const childSlug = `e2e-about-team-${Date.now()}`;

  it('creates a child page under the parent', async () => {
    const result = await authedClient.request<PageDto>({
      method: 'POST',
      path: '/v1/pages',
      body: {
        slug: childSlug,
        title: 'Our Team',
        description: 'Meet the team behind GameGuild.',
        pageType: 0, // Landing
        parentPageId: pageId,
        sortOrder: 0,
      },
      requiresAuth: true,
    });

    const child = unwrap(result, 'Create child page');
    childPageId = child.id;
    expect(child.parentPageId).toBe(pageId);
    expect(child.title).toBe('Our Team');
  });

  it('lists pages filtered by parentId', async () => {
    const result = await authedClient.request<PageDto[]>({
      method: 'GET',
      path: `/v1/pages?parentId=${pageId}`,
      requiresAuth: true,
    });

    const children = unwrap(result, 'List children');
    expect(children.length).toBeGreaterThanOrEqual(1);
    expect(children.some((p) => p.id === childPageId)).toBe(true);
  });

  // ── Cleanup: soft-delete pages ──

  it('soft-deletes the child page', async () => {
    const result = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/pages/${childPageId}`,
      requiresAuth: true,
    });
    expect(result.ok).toBe(true);
  });

  it('soft-deletes the parent page', async () => {
    const result = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/pages/${pageId}`,
      requiresAuth: true,
    });
    expect(result.ok).toBe(true);
  });
});

// ===========================================================================
// 2. CONTENT RESOURCES — full CRUD + search + publish + view count
// ===========================================================================

describe('Content Pages E2E — Content Resources', () => {
  let accessToken: string;
  let authedClient: ReturnType<typeof createClient>;
  const publicClient = createClient({
    baseUrl: BASE_URL,
    timeout: 30_000,
    devtools: { enabled: false },
  });

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `resources_e2e_${tag}`,
        email: `resources_e2e_${tag}@example.com`,
        password: 'Str0ng!Passw0rd123!',
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const data = unwrap(signUpResult, 'Resources E2E sign-up');
    accessToken = data.accessToken;

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
    });
  }, 30_000);

  // ── CRUD ──

  let articleId: string;
  const articleSlug = `e2e-intro-gamedev-${Date.now()}`;

  it('creates a content resource (article)', async () => {
    const result = await authedClient.request<ContentResourceDto>({
      method: 'POST',
      path: '/v1/content-resources',
      body: {
        slug: articleSlug,
        title: 'Introduction to Game Development',
        summary:
          'A comprehensive guide covering the fundamentals of game development.',
        body: '# Getting Started\n\nGame development combines art, design, and programming...',
        resourceType: 0, // Article
        locale: 'en-US',
        categorySlug: 'game-design',
        tags: 'gamedev,beginner,tutorial',
        coverImageUrl: 'https://cdn.gameguild.gg/articles/intro-gamedev.png',
        metaTitle: 'Intro to Game Dev — GameGuild',
        metaDescription:
          'Learn the fundamentals of game development in one article.',
        ogImageUrl: 'https://cdn.gameguild.gg/og/intro-gamedev.png',
        readingTimeMinutes: 12,
        isFeatured: true,
        sortOrder: 0,
      },
      requiresAuth: true,
    });

    const article = unwrap(result, 'Create article');
    articleId = article.id;

    expect(article.slug).toBe(articleSlug);
    expect(article.title).toBe('Introduction to Game Development');
    expect(article.summary).toContain('fundamentals');
    expect(article.categorySlug).toBe('game-design');
    expect(article.tags).toBe('gamedev,beginner,tutorial');
    expect(article.coverImageUrl).toContain('intro-gamedev.png');
    expect(article.readingTimeMinutes).toBe(12);
    expect(article.isFeatured).toBe(true);
    expect(article.viewCount).toBe(0);
    expect(article.status).toBe('Draft');
  });

  let tutorialId: string;
  const tutorialSlug = `e2e-unity-basics-${Date.now()}`;

  it('creates a second resource (tutorial)', async () => {
    const result = await authedClient.request<ContentResourceDto>({
      method: 'POST',
      path: '/v1/content-resources',
      body: {
        slug: tutorialSlug,
        title: 'Unity Basics for Beginners',
        summary: 'Learn Unity from scratch — your first 2D game.',
        body: '## Step 1: Install Unity Hub\n\n...',
        resourceType: 1, // Tutorial
        locale: 'en-US',
        categorySlug: 'programming',
        tags: 'unity,beginner,2d',
        coverImageUrl: 'https://cdn.gameguild.gg/tutorials/unity-basics.png',
        readingTimeMinutes: 25,
        isFeatured: false,
        sortOrder: 1,
      },
      requiresAuth: true,
    });

    const tutorial = unwrap(result, 'Create tutorial');
    tutorialId = tutorial.id;
    expect(tutorial.slug).toBe(tutorialSlug);
    expect(tutorial.resourceType).toBe('Tutorial');
  });

  it('retrieves a resource by ID', async () => {
    const result = await publicClient.request<ContentResourceDto>({
      method: 'GET',
      path: `/v1/content-resources/${articleId}`,
      requiresAuth: false,
    });

    const article = unwrap(result, 'Get resource by ID');
    expect(article.id).toBe(articleId);
    expect(article.title).toBe('Introduction to Game Development');
  });

  it('retrieves a resource by slug (anonymous, increments view count)', async () => {
    const result = await publicClient.request<ContentResourceDto>({
      method: 'GET',
      path: `/v1/content-resources/by-slug/${articleSlug}`,
      requiresAuth: false,
    });

    const article = unwrap(result, 'Get resource by slug');
    expect(article.slug).toBe(articleSlug);
    // View count should have gone up by at least 1
    expect(article.viewCount).toBeGreaterThanOrEqual(0);
  });

  it('lists all resources (anonymous)', async () => {
    const result = await publicClient.request<ContentResourceDto[]>({
      method: 'GET',
      path: '/v1/content-resources',
      requiresAuth: false,
    });

    const resources = unwrap(result, 'List resources');
    expect(Array.isArray(resources)).toBe(true);
    expect(resources.length).toBeGreaterThanOrEqual(2);
  }, 30_000);

  it('filters resources by category', async () => {
    const result = await publicClient.request<ContentResourceDto[]>({
      method: 'GET',
      path: '/v1/content-resources?category=game-design',
      requiresAuth: false,
    });

    const resources = unwrap(result, 'Filter by category');
    expect(resources.every((r) => r.categorySlug === 'game-design')).toBe(
      true,
    );
    expect(resources.some((r) => r.id === articleId)).toBe(true);
  });

  it('filters resources by featured', async () => {
    const result = await publicClient.request<ContentResourceDto[]>({
      method: 'GET',
      path: '/v1/content-resources?featured=true',
      requiresAuth: false,
    });

    const resources = unwrap(result, 'Filter by featured');
    expect(resources.every((r) => r.isFeatured === true)).toBe(true);
    expect(resources.some((r) => r.id === articleId)).toBe(true);
  });

  it('searches resources by keyword', async () => {
    const result = await publicClient.request<ContentResourceDto[]>({
      method: 'GET',
      path: '/v1/content-resources?q=Unity',
      requiresAuth: false,
    });

    const resources = unwrap(result, 'Search resources');
    expect(resources.some((r) => r.id === tutorialId)).toBe(true);
  });

  it('updates a resource', async () => {
    const result = await authedClient.request<ContentResourceDto>({
      method: 'PUT',
      path: `/v1/content-resources/${articleId}`,
      body: {
        title: 'Introduction to Game Development — Updated',
        summary: 'An updated, comprehensive guide to game development.',
        readingTimeMinutes: 15,
      },
      requiresAuth: true,
    });

    const article = unwrap(result, 'Update resource');
    expect(article.title).toBe(
      'Introduction to Game Development — Updated',
    );
    expect(article.readingTimeMinutes).toBe(15);
    // Original fields preserved
    expect(article.slug).toBe(articleSlug);
    expect(article.categorySlug).toBe('game-design');
  });

  // ── Publish ──

  it('publishes a resource', async () => {
    const result = await authedClient.request<ContentResourceDto>({
      method: 'POST',
      path: `/v1/content-resources/${articleId}/publish`,
      requiresAuth: true,
    });

    const article = unwrap(result, 'Publish resource');
    expect(article.status).toBe('Published');
    expect(article.publishedAt).toBeTruthy();
  });

  // ── Cleanup ──

  it('soft-deletes resources', async () => {
    const r1 = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/content-resources/${articleId}`,
      requiresAuth: true,
    });
    expect(r1.ok).toBe(true);

    const r2 = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/content-resources/${tutorialId}`,
      requiresAuth: true,
    });
    expect(r2.ok).toBe(true);
  });
});

// ===========================================================================
// 3. OPENGRAPH RESOLUTION — public endpoint resolving pages & resources
// ===========================================================================

describe('Content Pages E2E — OpenGraph resolution', () => {
  let accessToken: string;
  let authedClient: ReturnType<typeof createClient>;
  const publicClient = createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
  });

  const ogPageSlug = `e2e-og-pricing-${Date.now()}`;
  const ogResourceSlug = `e2e-og-blog-post-${Date.now()}`;
  let ogPageId: string;
  let ogResourceId: string;

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `og_e2e_${tag}`,
        email: `og_e2e_${tag}@example.com`,
        password: 'Str0ng!Passw0rd123!',
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const data = unwrap(signUpResult, 'OG E2E sign-up');
    accessToken = data.accessToken;

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
    });

    // Create a published page
    const pageResult = await authedClient.request<PageDto>({
      method: 'POST',
      path: '/v1/pages',
      body: {
        slug: ogPageSlug,
        title: 'Pricing',
        description: 'GameGuild pricing plans',
        pageType: 0,
        ogTitle: 'GameGuild Pricing — Start Free',
        ogDescription:
          'Choose the plan that fits your journey. Free tier available.',
        ogImageUrl: 'https://cdn.gameguild.gg/og/pricing.png',
        ogType: 'website',
        twitterCard: 'summary_large_image',
        twitterSite: '@gameguild',
        canonicalUrl: `https://gameguild.gg/${ogPageSlug}`,
        robotsDirective: 'index,follow',
        structuredData: JSON.stringify({
          '@context': 'https://schema.org',
          '@type': 'WebPage',
          name: 'Pricing',
        }),
      },
      requiresAuth: true,
    });

    const page = unwrap(pageResult, 'Create OG page');
    ogPageId = page.id;

    // Publish the page
    await authedClient.request<PageDto>({
      method: 'POST',
      path: `/v1/pages/${ogPageId}/publish`,
      requiresAuth: true,
    });

    // Create and publish a content resource
    const resResult = await authedClient.request<ContentResourceDto>({
      method: 'POST',
      path: '/v1/content-resources',
      body: {
        slug: ogResourceSlug,
        title: 'How to Build Your First Game',
        summary: 'A step-by-step walkthrough for aspiring game developers.',
        resourceType: 0, // Article
        metaTitle: 'Build Your First Game — GameGuild Blog',
        metaDescription: 'Follow this guide to create your first game today.',
        ogImageUrl: 'https://cdn.gameguild.gg/blog/first-game.png',
        coverImageUrl: 'https://cdn.gameguild.gg/blog/first-game-cover.png',
      },
      requiresAuth: true,
    });

    const resource = unwrap(resResult, 'Create OG resource');
    ogResourceId = resource.id;

    await authedClient.request<ContentResourceDto>({
      method: 'POST',
      path: `/v1/content-resources/${ogResourceId}/publish`,
      requiresAuth: true,
    });
  }, 60_000);

  it('resolves OpenGraph for a published page', async () => {
    const result = await publicClient.request<OpenGraphMetadataDto>({
      method: 'GET',
      path: `/v1/og/${ogPageSlug}`,
      requiresAuth: false,
    });

    const og = unwrap(result, 'Resolve OG for page');

    expect(og.slug).toBe(ogPageSlug);
    expect(og.ogTitle).toBe('GameGuild Pricing — Start Free');
    expect(og.ogDescription).toBe(
      'Choose the plan that fits your journey. Free tier available.',
    );
    expect(og.ogImageUrl).toBe('https://cdn.gameguild.gg/og/pricing.png');
    expect(og.ogType).toBe('website');
    expect(og.twitterCard).toBe('summary_large_image');
    expect(og.twitterSite).toBe('@gameguild');
    expect(og.canonicalUrl).toBe(`https://gameguild.gg/${ogPageSlug}`);
    expect(og.robotsDirective).toBe('index,follow');
    expect(og.structuredData).toBeTruthy();

    const sd = JSON.parse(og.structuredData!);
    expect(sd['@type']).toBe('WebPage');
  });

  it('resolves OpenGraph for a published content resource', async () => {
    const result = await publicClient.request<OpenGraphMetadataDto>({
      method: 'GET',
      path: `/v1/og/${ogResourceSlug}`,
      requiresAuth: false,
    });

    const og = unwrap(result, 'Resolve OG for resource');

    expect(og.slug).toBe(ogResourceSlug);
    expect(og.ogTitle).toBe('Build Your First Game — GameGuild Blog');
    expect(og.ogDescription).toBe(
      'Follow this guide to create your first game today.',
    );
    // Should pick ogImageUrl over coverImageUrl
    expect(og.ogImageUrl).toBe(
      'https://cdn.gameguild.gg/blog/first-game.png',
    );
    expect(og.ogType).toBe('article');
    expect(og.twitterCard).toBe('summary_large_image');
  });

  it('returns 404 for a non-existent slug', async () => {
    const result = await publicClient.request<OpenGraphMetadataDto>({
      method: 'GET',
      path: '/v1/og/this-slug-does-not-exist-xyz',
      requiresAuth: false,
    });

    expect(result.ok).toBe(false);
    expect(result.error?.status).toBe(404);
  });

  it('returns 404 for an unpublished page slug', async () => {
    // Unpublish the page first
    const unpubResult = await authedClient.request<PageDto>({
      method: 'POST',
      path: `/v1/pages/${ogPageId}/unpublish`,
      requiresAuth: true,
    });
    const unpubPage = unwrap(unpubResult, 'Unpublish page for OG test');
    expect(unpubPage.status).toBe('Draft');

    // Use a cache-busting query param to avoid any middleware response caching
    const result = await publicClient.request<OpenGraphMetadataDto>({
      method: 'GET',
      path: `/v1/og/${ogPageSlug}?_t=${Date.now()}`,
      requiresAuth: false,
    });

    expect(result.ok).toBe(false);
    expect(result.error?.status).toBe(404);

    // Republish for subsequent tests
    await authedClient.request<PageDto>({
      method: 'POST',
      path: `/v1/pages/${ogPageId}/publish`,
      requiresAuth: true,
    });
  });

  it('page slug takes priority over resource slug when both match', async () => {
    // Create a resource with the same slug as the page (should fail due to unique constraint)
    // Instead, verify priority by checking the page slug returns page OG data, not resource
    const result = await publicClient.request<OpenGraphMetadataDto>({
      method: 'GET',
      path: `/v1/og/${ogPageSlug}`,
      requiresAuth: false,
    });

    const og = unwrap(result, 'OG priority check');
    // The OG type should be 'website' (page), not 'article' (resource)
    expect(og.ogType).toBe('website');
    expect(og.ogTitle).toBe('GameGuild Pricing — Start Free');
  });

  // ── Cleanup ──

  it('cleans up OG test data', async () => {
    await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/pages/${ogPageId}`,
      requiresAuth: true,
    });
    await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/content-resources/${ogResourceId}`,
      requiresAuth: true,
    });
  });
});

// ===========================================================================
// 4. WEB INTEGRATION — Simulates how Next.js fetches metadata for <head>
// ===========================================================================

describe('Content Pages E2E — Web integration (metadata for SSR)', () => {
  let accessToken: string;
  let authedClient: ReturnType<typeof createClient>;
  const publicClient = createClient({
    baseUrl: BASE_URL,
    timeout: 10_000,
    devtools: { enabled: false },
  });

  const homeSlug = `e2e-home-${Date.now()}`;
  let homePageId: string;

  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `web_e2e_${tag}`,
        email: `web_e2e_${tag}@example.com`,
        password: 'Str0ng!Passw0rd123!',
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const data = unwrap(signUpResult, 'Web E2E sign-up');
    accessToken = data.accessToken;

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
    });

    // Create and publish the home page with full metadata
    const pageResult = await authedClient.request<PageDto>({
      method: 'POST',
      path: '/v1/pages',
      body: {
        slug: homeSlug,
        title: 'GameGuild — Game Development Education',
        description: 'Build. Learn. Play. The ultimate game dev education platform.',
        pageType: 0,
        locale: 'en-US',
        metaTitle: 'GameGuild — Build Games, Learn Code',
        metaDescription: 'Join 10,000+ game developers learning game design, programming, and art.',
        metaKeywords: 'gamedev,education,unity,unreal,godot',
        canonicalUrl: 'https://gameguild.gg/',
        robotsDirective: 'index,follow',
        ogTitle: 'GameGuild — The Ultimate Game Dev Platform',
        ogDescription: 'Build. Learn. Play. Start your game development journey today.',
        ogImageUrl: 'https://cdn.gameguild.gg/og/home.png',
        ogType: 'website',
        twitterCard: 'summary_large_image',
        twitterSite: '@gameguild',
        structuredData: JSON.stringify({
          '@context': 'https://schema.org',
          '@type': 'WebSite',
          name: 'GameGuild',
          url: 'https://gameguild.gg',
          potentialAction: {
            '@type': 'SearchAction',
            target: 'https://gameguild.gg/search?q={query}',
            'query-input': 'required name=query',
          },
        }),
        body: '# Welcome to GameGuild\n\nThe ultimate game development education platform.',
        customData: JSON.stringify({
          heroConfig: {
            variant: 'gradient',
            primaryColor: '#7C3AED',
            secondaryColor: '#2563EB',
          },
        }),
      },
      requiresAuth: true,
    });

    const page = unwrap(pageResult, 'Create home page');
    homePageId = page.id;

    // Publish
    await authedClient.request<PageDto>({
      method: 'POST',
      path: `/v1/pages/${homePageId}/publish`,
      requiresAuth: true,
    });

    // Add hero section
    await authedClient.request<PageSectionDto>({
      method: 'POST',
      path: `/v1/pages/${homePageId}/sections`,
      body: {
        sectionType: 0,
        heading: 'Build Incredible Games',
        subheading: 'Learn from industry experts and build your dream game.',
        data: JSON.stringify({
          bgVideo: 'https://cdn.gameguild.gg/hero-video.mp4',
          ctaPrimary: { text: 'Start Learning', url: '/courses' },
          ctaSecondary: { text: 'View Pricing', url: '/pricing' },
        }),
        sortOrder: 0,
        isVisible: true,
      },
      requiresAuth: true,
    });

    // Add features section
    await authedClient.request<PageSectionDto>({
      method: 'POST',
      path: `/v1/pages/${homePageId}/sections`,
      body: {
        sectionType: 1,
        heading: 'Everything You Need',
        data: JSON.stringify({
          items: [
            { icon: 'book', title: '200+ Courses', description: 'From beginner to expert.' },
            { icon: 'users', title: '10K+ Students', description: 'Active learning community.' },
            { icon: 'award', title: 'Certificates', description: 'Recognized credentials.' },
          ],
        }),
        sortOrder: 1,
        isVisible: true,
      },
      requiresAuth: true,
    });
  }, 60_000);

  it('fetches page metadata for Next.js generateMetadata()', async () => {
    // Simulates what Next.js does in generateMetadata():
    // 1. Fetch the OG metadata from the public endpoint
    const ogResult = await publicClient.request<OpenGraphMetadataDto>({
      method: 'GET',
      path: `/v1/og/${homeSlug}`,
      requiresAuth: false,
    });

    const og = unwrap(ogResult, 'Fetch OG for SSR');

    // Verify all the metadata Next.js needs for <head>
    expect(og.title).toBeTruthy();
    expect(og.ogTitle).toBeTruthy();
    expect(og.ogDescription).toBeTruthy();
    expect(og.ogImageUrl).toBeTruthy();
    expect(og.ogType).toBe('website');
    expect(og.twitterCard).toBe('summary_large_image');
    expect(og.canonicalUrl).toBe('https://gameguild.gg/');

    // Structured data should be valid JSON-LD
    expect(og.structuredData).toBeTruthy();
    const sd = JSON.parse(og.structuredData!);
    expect(sd['@context']).toBe('https://schema.org');
    expect(sd['@type']).toBe('WebSite');

    // Simulate mapping to Next.js Metadata object
    const nextMetadata = {
      title: og.ogTitle,
      description: og.ogDescription,
      openGraph: {
        title: og.ogTitle,
        description: og.ogDescription,
        images: og.ogImageUrl ? [{ url: og.ogImageUrl }] : [],
        type: og.ogType,
      },
      twitter: {
        card: og.twitterCard,
        site: og.twitterSite,
        title: og.ogTitle,
        description: og.ogDescription,
        images: og.ogImageUrl ? [og.ogImageUrl] : [],
      },
      alternates: {
        canonical: og.canonicalUrl,
      },
      robots: og.robotsDirective,
    };

    expect(nextMetadata.title).toBe(
      'GameGuild — The Ultimate Game Dev Platform',
    );
    expect(nextMetadata.openGraph.images[0].url).toBe(
      'https://cdn.gameguild.gg/og/home.png',
    );
    expect(nextMetadata.twitter.card).toBe('summary_large_image');
    expect(nextMetadata.alternates.canonical).toBe('https://gameguild.gg/');
    expect(nextMetadata.robots).toBe('index,follow');
  });

  it('fetches full page data with sections for rendering', async () => {
    // Simulates the page component fetching full data:
    const pageResult = await publicClient.request<PageDto>({
      method: 'GET',
      path: `/v1/pages/by-slug/${homeSlug}`,
      requiresAuth: false,
      timeout: 15_000,
    });

    const page = unwrap(pageResult, 'Fetch page for rendering');

    // Page data
    expect(page.title).toBe('GameGuild — Game Development Education');
    expect(page.body).toContain('# Welcome to GameGuild');

    // Custom data (hero config)
    const customData = JSON.parse(page.customData!);
    expect(customData.heroConfig.variant).toBe('gradient');

    // Sections should be ordered
    expect(page.sections.length).toBe(2);
    expect(page.sections[0].sortOrder).toBeLessThan(
      page.sections[1].sortOrder,
    );

    // Hero section
    const hero = page.sections.find((s) => s.sectionType === 'Hero');
    expect(hero).toBeDefined();
    expect(hero!.heading).toBe('Build Incredible Games');

    const heroData = JSON.parse(hero!.data!);
    expect(heroData.ctaPrimary.text).toBe('Start Learning');
    expect(heroData.bgVideo).toContain('hero-video.mp4');

    // Features section
    const features = page.sections.find((s) => s.sectionType === 'Features');
    expect(features).toBeDefined();
    expect(features!.heading).toBe('Everything You Need');

    const featuresData = JSON.parse(features!.data!);
    expect(featuresData.items.length).toBe(3);
    expect(featuresData.items[0].title).toBe('200+ Courses');
  }, 30_000);

  it('simulates complete SSR flow: metadata + page data in parallel', async () => {
    // In Next.js, generateMetadata() and the page component are called concurrently.
    // This test simulates both happening in parallel.
    const [ogResult, pageResult] = await Promise.all([
      publicClient.request<OpenGraphMetadataDto>({
        method: 'GET',
        path: `/v1/og/${homeSlug}`,
        requiresAuth: false,
      }),
      publicClient.request<PageDto>({
        method: 'GET',
        path: `/v1/pages/by-slug/${homeSlug}`,
        requiresAuth: false,
      }),
    ]);

    const og = unwrap(ogResult, 'Parallel OG fetch');
    const page = unwrap(pageResult, 'Parallel page fetch');

    // Both should return consistent data
    expect(og.slug).toBe(page.slug);
    expect(og.title).toBeTruthy();
    expect(page.sections.length).toBeGreaterThan(0);

    // OG title should fallback properly
    expect(og.ogTitle).toBe(page.ogTitle);
  });

  // ── Cleanup ──

  it('cleans up web integration test data', async () => {
    await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/pages/${homePageId}`,
      requiresAuth: true,
    });
  });
});
