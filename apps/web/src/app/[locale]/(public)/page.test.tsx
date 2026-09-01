import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: vi.fn().mockResolvedValue(null) }));
vi.mock('@/components/feed/feed-shell', () => ({
  FeedShell: ({ tab }: { tab?: string }) => <div data-testid="feed-shell" data-tab={tab ?? 'foryou'} />,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/community/public-community-queries', () => ({
  getPublicActivities: vi.fn().mockResolvedValue([]),
  getPublicMemberSpotlights: vi.fn().mockResolvedValue([]),
  getPublicPlaytests: vi.fn().mockResolvedValue([]),
}));
vi.mock('@/lib/projects/public-projects', () => ({
  getPublishedProjects: vi.fn().mockResolvedValue([]),
}));

import RootPage from './page';

const props = { params: Promise.resolve({ locale: 'en-US' }) } as never;

describe('contextual root page', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders the community feed for signed-in members', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });

    render(await RootPage(props));

    expect(screen.getByTestId('feed-shell')).toHaveAttribute('data-tab', 'foryou');
  });

  it('passes the requested tab to the feed', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });

    render(
      await RootPage({
        params: Promise.resolve({ locale: 'en-US' }),
        searchParams: Promise.resolve({ tab: 'trending' }),
      } as never),
    );

    expect(screen.getByTestId('feed-shell')).toHaveAttribute('data-tab', 'trending');
  });

  it('renders the marketing landing for anonymous visitors', async () => {
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.queryByTestId('feed-shell')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Learn, Build & Connect' })).toBeInTheDocument();
  });
});
