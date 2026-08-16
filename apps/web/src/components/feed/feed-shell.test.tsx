import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  loadPosts: vi.fn(),
}));

vi.mock('@/lib/posts/queries', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/posts/queries')>()),
  loadPosts: mocks.loadPosts,
}));
vi.mock('@/components/feed/infinite-post-feed', () => ({
  InfinitePostFeed: ({ stream, initialItems, initialNextSkip }: { stream: string; initialItems: unknown[]; initialNextSkip: number | null }) => (
    <div
      data-testid="infinite-feed"
      data-stream={stream}
      data-count={initialItems.length}
      data-next-skip={initialNextSkip ?? 'null'}
    />
  ),
}));
vi.mock('@/components/feed/upcoming-playtests', () => ({
  UpcomingPlaytests: () => <div data-testid="playtests-rail" />,
}));
vi.mock('@/components/feed/featured-projects', () => ({
  FeaturedProjects: () => <div data-testid="featured-rail" />,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...rest }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...rest}>{children}</a>
  ),
}));

import { FeedShell } from './feed-shell';

const posts = Array.from({ length: 6 }, (_, i) => ({
  id: `post-${i}`,
  authorId: 'a',
  authorName: null,
  content: 'x',
  mediaUrl: null,
  mediaType: null,
  likesCount: 0,
  commentsCount: 0,
  createdAt: new Date().toISOString(),
}));

describe('FeedShell', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders all four tabs with the active one marked', async () => {
    mocks.loadPosts.mockResolvedValue([]);

    render(await FeedShell({ tab: 'trending' }));

    expect(screen.getByRole('link', { name: 'Trending', current: 'page' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'For You' })).toHaveAttribute('href', '/');
    expect(screen.getByRole('link', { name: 'Following' })).toHaveAttribute('href', '/?tab=following');
  });

  it('maps each tab to its posts stream with the SSR page', async () => {
    mocks.loadPosts.mockResolvedValue(posts);

    render(await FeedShell({ tab: 'trending' }));

    expect(mocks.loadPosts).toHaveBeenCalledWith('trending', 0);
    expect(screen.getByTestId('infinite-feed')).toHaveAttribute('data-stream', 'trending');
    expect(screen.getByTestId('infinite-feed')).toHaveAttribute('data-count', '6');
  });

  it('passes a null next page when the first page is short', async () => {
    mocks.loadPosts.mockResolvedValue(posts.slice(0, 3));

    render(await FeedShell({ tab: 'discover' }));

    expect(mocks.loadPosts).toHaveBeenCalledWith('public', 0);
    expect(screen.getByTestId('infinite-feed')).toHaveAttribute('data-next-skip', 'null');
  });

  it('shows the building-feed empty state for personal tabs without posts', async () => {
    mocks.loadPosts.mockResolvedValue([]);

    render(await FeedShell({ tab: 'foryou' }));

    expect(screen.getByText('Your feed is building')).toBeInTheDocument();
    expect(screen.queryByTestId('infinite-feed')).not.toBeInTheDocument();
  });

  it('renders the suggestions rail', async () => {
    mocks.loadPosts.mockResolvedValue(posts);

    render(await FeedShell({ tab: 'foryou' }));

    expect(screen.getByTestId('playtests-rail')).toBeInTheDocument();
    expect(screen.getByTestId('featured-rail')).toBeInTheDocument();
  });
});
