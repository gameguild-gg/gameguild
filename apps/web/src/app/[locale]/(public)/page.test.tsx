import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/components/feed/feed-shell', () => ({
  FeedShell: () => <div data-testid="feed-shell" />,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

import RootPage from './page';

const props = { params: Promise.resolve({ locale: 'en-US' }) } as never;

describe('contextual root page', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders the community feed for signed-in members', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });

    render(await RootPage(props));

    expect(screen.getByTestId('feed-shell')).toBeInTheDocument();
  });

  it('renders the marketing landing for anonymous visitors', async () => {
    mocks.auth.mockResolvedValue(null);

    render(await RootPage(props));

    expect(screen.queryByTestId('feed-shell')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Learn, Build & Connect' })).toBeInTheDocument();
  });
});
