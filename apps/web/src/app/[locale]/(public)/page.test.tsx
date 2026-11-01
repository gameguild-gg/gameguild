import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  redirect: vi.fn((args: unknown) => {
    throw new Error(`redirect:${JSON.stringify(args)}`);
  }),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: vi.fn().mockResolvedValue(null) }));
vi.mock('next-intl/server', () => ({
  getTranslations: async (namespace: string) => (key: string) => `${namespace}.${key}`,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
  redirect: mocks.redirect,
}));
vi.mock('@/lib/community/public-community-queries', () => ({
  getPublicActivities: vi.fn().mockResolvedValue([]),
  getPublicMemberSpotlights: vi.fn().mockResolvedValue([]),
  getPublicPlaytests: vi.fn().mockResolvedValue([]),
}));
vi.mock('@/lib/projects/public-projects', () => ({
  getPublishedProjects: vi.fn().mockResolvedValue([]),
  PUBLIC_PROJECT_PAGE_SIZE: 24,
}));
vi.mock('@/lib/courses/services/course.service', () => ({
  getPublicCourseCatalog: vi.fn().mockResolvedValue({ success: true, data: [] }),
}));
vi.mock('@/lib/blogs/queries', () => ({
  getBlogIndex: vi.fn().mockResolvedValue({ items: [], hasMore: false }),
}));

import RootPage from './page';

const props = { params: Promise.resolve({ locale: 'en-US' }) } as never;

describe('contextual root page', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('sends signed-in members to the social shell', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });

    await expect(RootPage(props)).rejects.toThrow(
      'redirect:{"href":"/feed","locale":"en-US"}',
    );
  });

  it('preserves a supported social tab during the redirect', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });

    await expect(
      RootPage({
        params: Promise.resolve({ locale: 'en-US' }),
        searchParams: Promise.resolve({ tab: 'following' }),
      } as never),
    ).rejects.toThrow(
      'redirect:{"href":"/feed?tab=following","locale":"en-US"}',
    );
  });

  it('renders the marketing landing for anonymous visitors', async () => {
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.getByRole('heading', { name: 'Learn, Build & Connect' })).toBeInTheDocument();
  });

  it('lists the latest courses and community posts (issue #123)', async () => {
    const { getPublicCourseCatalog } = await import('@/lib/courses/services/course.service');
    const { getBlogIndex } = await import('@/lib/blogs/queries');
    vi.mocked(getPublicCourseCatalog).mockResolvedValue({
      success: true,
      data: [
        {
          id: 'course-1',
          slug: 'game-feel-foundations',
          title: 'Game Feel Foundations',
          description: 'Juice your prototypes.',
          difficulty: 'Beginner',
          estimatedHours: 12,
        },
      ],
    } as never);
    vi.mocked(getBlogIndex).mockResolvedValue({
      items: [
        {
          id: 'post-1',
          slug: 'shipping-a-jam-game',
          title: 'Shipping a Jam Game',
          excerpt: 'Lessons from a 48h jam.',
          primaryAuthorHandle: 'devperson',
          primaryAuthorDisplayName: 'Dev Person',
          publishedAt: '2026-10-01T00:00:00.000Z',
          readTimeMinutes: 5,
        },
      ],
      hasMore: false,
    } as never);
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.getByRole('heading', { name: 'Latest courses' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Latest from the community' })).toBeInTheDocument();
    expect(screen.getByText('Game Feel Foundations')).toBeInTheDocument();
    expect(screen.getByText('Shipping a Jam Game')).toBeInTheDocument();
  });

  it('communicates every platform feature including game jams and mentorship (issue #123)', async () => {
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.getByRole('heading', { name: 'Everything You Need to Succeed' })).toBeInTheDocument();
    for (const feature of [
      'Interactive Courses',
      'Game Jams',
      'Community Studio',
      'Mentorship',
      'Testing Lab',
      'Launch Pad',
    ]) {
      expect(screen.getByRole('heading', { name: feature })).toBeInTheDocument();
    }
    // Feature cards double as navigation to the live public surfaces.
    expect(screen.getByRole('link', { name: /join a jam/i })).toHaveAttribute('href', '/community');
    expect(screen.getByRole('link', { name: /find a mentor/i })).toHaveAttribute('href', '/jobs');
  });

  it('renders the community stats band derived from live public data (issue #123)', async () => {
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    const stats = screen.getByRole('region', { name: 'Community stats' });
    expect(stats).toBeInTheDocument();
    expect(screen.getByText('Published courses')).toBeInTheDocument();
    expect(screen.getByText('Projects in the showcase')).toBeInTheDocument();
    expect(screen.getByText('Upcoming playtests')).toBeInTheDocument();
    expect(screen.getByText('Members in the spotlight')).toBeInTheDocument();
  });

  it('presents a strong sign-up call to action (issue #123)', async () => {
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.getByRole('link', { name: /join gameguild/i })).toHaveAttribute('href', '/sign-up');
    const cta = screen.getByRole('region', { name: 'Join GameGuild' });
    expect(within(cta).getByRole('heading', { name: 'Ready to ship your first game?' })).toBeInTheDocument();
    expect(within(cta).getByRole('link', { name: /create your free account/i })).toHaveAttribute(
      'href',
      '/sign-up',
    );
  });

  it('keeps the landing usable when the course catalog or blog are unavailable', async () => {
    const { getPublicCourseCatalog } = await import('@/lib/courses/services/course.service');
    const { getBlogIndex } = await import('@/lib/blogs/queries');
    vi.mocked(getPublicCourseCatalog).mockRejectedValue(new Error('api down'));
    vi.mocked(getBlogIndex).mockRejectedValue(new Error('api down'));
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.getByText('Published courses will appear here soon.')).toBeInTheDocument();
    expect(screen.getByText('Community posts will appear here soon.')).toBeInTheDocument();
  });
});
