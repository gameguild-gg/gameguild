import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  dismissFeedItemAction: vi.fn().mockResolvedValue(undefined),
  markFeedItemViewedAction: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('@/lib/feed/feed-actions', () => ({
  dismissFeedItemAction: mocks.dismissFeedItemAction,
  markFeedItemViewedAction: mocks.markFeedItemViewedAction,
}));

import { FeedUpdateCard } from './feed-update-card';
import type { PersonalFeedItem } from '@/lib/feed/personalized-feed';

const unreadItem: PersonalFeedItem = {
  id: 'item-1',
  title: 'New course matches your interests',
  reason: 'New course matches your interests',
  kind: 'New course',
  href: '/courses/ai-4-games',
  relevanceScore: 9.5,
  isViewed: false,
  createdAt: '2026-08-01T10:00:00.000Z',
};

describe('FeedUpdateCard', () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(cleanup);

  it('renders the update with an unread indicator and link', () => {
    render(<FeedUpdateCard item={unreadItem} />);

    expect(screen.getByRole('link', { name: /new course matches your interests/i })).toHaveAttribute(
      'href',
      '/courses/ai-4-games',
    );
    expect(screen.getByLabelText('Unread')).toBeInTheDocument();
  });

  it('marks the item viewed on first click', async () => {
    const user = userEvent.setup();
    render(<FeedUpdateCard item={unreadItem} />);

    await user.click(screen.getByRole('link', { name: /new course matches your interests/i }));

    expect(mocks.markFeedItemViewedAction).toHaveBeenCalledWith('item-1');
  });

  it('does not re-mark viewed items on click', async () => {
    const user = userEvent.setup();
    render(<FeedUpdateCard item={{ ...unreadItem, isViewed: true }} />);

    await user.click(screen.getByRole('link', { name: /new course matches your interests/i }));

    expect(mocks.markFeedItemViewedAction).not.toHaveBeenCalled();
  });

  it('removes the card and dismisses through the server action', async () => {
    const user = userEvent.setup();
    render(<FeedUpdateCard item={unreadItem} />);

    await user.click(screen.getByRole('button', { name: 'Dismiss update' }));

    expect(screen.queryByTestId('feed-update-item-1')).not.toBeInTheDocument();
    expect(mocks.dismissFeedItemAction).toHaveBeenCalledWith('item-1');
  });
});
