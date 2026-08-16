import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getPersonalizedFeed: vi.fn(),
  getCommunityFeed: vi.fn(),
}));

vi.mock('@/lib/feed/personalized-feed', () => ({
  getPersonalizedFeed: mocks.getPersonalizedFeed,
}));
vi.mock('@/lib/community/queries/members', () => ({
  getCommunityFeed: mocks.getCommunityFeed,
}));
vi.mock('@/components/feed/feed-update-card', () => ({
  FeedUpdateCard: ({ item }: { item: { id: string } }) => (
    <div data-testid={`feed-update-${item.id}`} />
  ),
}));
vi.mock('@/components/feed/upcoming-playtests', () => ({
  UpcomingPlaytests: () => <div data-testid="playtests-rail" />,
}));
vi.mock('@/components/feed/featured-projects', () => ({
  FeaturedProjects: () => <div data-testid="featured-rail" />,
}));
vi.mock('next/image', () => ({
  default: (props: Record<string, unknown>) => <img alt="" {...props} />,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...rest }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...rest}>{children}</a>
  ),
}));

import { FeedShell } from './feed-shell';

const communityItems = [
  {
    id: 'item-1',
    title: 'Neon Racer devlog #4',
    contentType: 'Project',
    contentId: 'neon-racer',
    authorId: 'ada-builder',
    reason: 'Recommended',
    relevanceScore: 9,
    isRead: false,
    createdAt: new Date().toISOString(),
    summary: 'New build drops Friday.',
    href: '/projects/neon-racer',
    imageUrl: 'https://example.com/cover.jpg',
    actionLabel: 'View project',
  },
  {
    id: 'item-2',
    title: 'Playtest recap',
    contentType: 'Post',
    contentId: 'post-2',
    authorId: 'grace-tester',
    reason: 'Trending',
    relevanceScore: 7,
    isRead: true,
    createdAt: new Date().toISOString(),
    summary: null,
  },
];

describe('FeedShell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCommunityFeed.mockResolvedValue({ kind: 'trending', requiresSignIn: false, items: communityItems });
    mocks.getPersonalizedFeed.mockResolvedValue({ requiresSignIn: false, items: [] });
  });
  afterEach(cleanup);

  it('renders all four tabs with the active one marked', async () => {
    render(await FeedShell({ tab: 'trending' }));

    const active = screen.getByRole('link', { name: 'Trending', current: 'page' });
    expect(active).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'For You' })).toHaveAttribute('href', '/');
    expect(screen.getByRole('link', { name: 'Following' })).toHaveAttribute('href', '/?tab=following');
  });

  it('streams community items as cards for social tabs', async () => {
    render(await FeedShell({ tab: 'trending' }));

    expect(mocks.getCommunityFeed).toHaveBeenCalledWith('trending', { take: 12 });
    expect(screen.getAllByTestId('feed-card')).toHaveLength(2);
    expect(screen.getByRole('link', { name: /neon racer devlog #4/i })).toHaveAttribute(
      'href',
      '/projects/neon-racer',
    );
  });

  it('renders personalized update cards for the foryou tab', async () => {
    mocks.getPersonalizedFeed.mockResolvedValue({
      requiresSignIn: false,
      items: [
        { id: 'p-1', title: 'New course', reason: 'r', kind: 'New course', href: null, relevanceScore: 1, isViewed: false, createdAt: null },
      ],
    });

    render(await FeedShell({ tab: 'foryou' }));

    expect(mocks.getPersonalizedFeed).toHaveBeenCalledWith(12);
    expect(screen.getByTestId('feed-update-p-1')).toBeInTheDocument();
    expect(screen.queryByTestId('feed-card')).not.toBeInTheDocument();
  });

  it('shows the sign-in prompt when following requires auth', async () => {
    mocks.getCommunityFeed.mockResolvedValue({ kind: 'following', requiresSignIn: true, items: [] });

    render(await FeedShell({ tab: 'following' }));

    expect(screen.getByText('Sign in to load this feed')).toBeInTheDocument();
  });

  it('shows an empty state per tab when no items exist', async () => {
    mocks.getCommunityFeed.mockResolvedValue({ kind: 'discover', requiresSignIn: false, items: [] });

    render(await FeedShell({ tab: 'discover' }));

    expect(screen.getByText(/recommended community updates will appear here/i)).toBeInTheDocument();
  });

  it('renders the suggestions rail', async () => {
    render(await FeedShell({ tab: 'trending' }));

    expect(screen.getByTestId('playtests-rail')).toBeInTheDocument();
    expect(screen.getByTestId('featured-rail')).toBeInTheDocument();
  });
});
