import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  getApiSocialFeedMe: vi.fn(),
  postApiSocialFeedDismiss: vi.fn(),
  postApiSocialFeedViewed: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    LearningExperienceSocialFeedModule: class {
      getApiSocialFeedMe = mocks.getApiSocialFeedMe;
      postApiSocialFeedDismiss = mocks.postApiSocialFeedDismiss;
      postApiSocialFeedViewed = mocks.postApiSocialFeedViewed;
    },
  },
}));

import {
  dismissPersonalFeedItem,
  getPersonalizedFeed,
  markPersonalFeedItemViewed,
} from './personalized-feed';

const session = { user: { id: 'user-1' } };

describe('getPersonalizedFeed', () => {
  it('requires sign-in for anonymous visitors', async () => {
    mocks.auth.mockResolvedValue(null);

    const feed = await getPersonalizedFeed();

    expect(feed).toEqual({ requiresSignIn: true, items: [] });
    expect(mocks.getApiSocialFeedMe).not.toHaveBeenCalled();
  });

  it('maps personalized feed items with course hrefs and labels', async () => {
    mocks.auth.mockResolvedValue(session);
    mocks.getApiSocialFeedMe.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'item-1',
          itemType: 'NewCourse',
          courseId: 'ai-4-games',
          reason: 'New course matches your interests',
          relevanceScore: 9.5,
          isViewed: false,
          createdAt: '2026-08-01T10:00:00.000Z',
        },
        {
          id: 'item-2',
          itemType: 'LearningPathSuggestion',
          learningPathId: 'path-1',
          reason: null,
          isViewed: true,
        },
      ],
    });

    const feed = await getPersonalizedFeed();

    expect(feed.requiresSignIn).toBe(false);
    expect(mocks.getApiSocialFeedMe).toHaveBeenCalledWith({ skip: 0, take: 10 });
    expect(feed.items).toEqual([
      {
        id: 'item-1',
        title: 'New course matches your interests',
        reason: 'New course matches your interests',
        kind: 'New course',
        href: '/courses/ai-4-games',
        relevanceScore: 9.5,
        isViewed: false,
        createdAt: '2026-08-01T10:00:00.000Z',
      },
      {
        id: 'item-2',
        title: 'Learning path for you',
        reason: null,
        kind: 'Learning path',
        href: '/courses',
        relevanceScore: 0,
        isViewed: true,
        createdAt: null,
      },
    ]);
  });

  it('returns an empty feed when the API fails', async () => {
    mocks.auth.mockResolvedValue(session);
    mocks.getApiSocialFeedMe.mockResolvedValue({ ok: false, error: { status: 500 } });

    const feed = await getPersonalizedFeed();

    expect(feed).toEqual({ requiresSignIn: false, items: [] });
  });

  it('survives API exceptions', async () => {
    mocks.auth.mockResolvedValue(session);
    mocks.getApiSocialFeedMe.mockRejectedValue(new Error('network down'));

    await expect(getPersonalizedFeed()).resolves.toEqual({ requiresSignIn: false, items: [] });
  });
});

describe('feed item lifecycle actions', () => {
  it('dismisses an item through the API', async () => {
    mocks.postApiSocialFeedDismiss.mockResolvedValue({ ok: true, data: {} });

    await expect(dismissPersonalFeedItem('item-1')).resolves.toBe(true);
    expect(mocks.postApiSocialFeedDismiss).toHaveBeenCalledWith('item-1');
  });

  it('marks an item viewed through the API', async () => {
    mocks.postApiSocialFeedViewed.mockResolvedValue({ ok: true, data: {} });

    await expect(markPersonalFeedItemViewed('item-1')).resolves.toBe(true);
    expect(mocks.postApiSocialFeedViewed).toHaveBeenCalledWith('item-1');
  });

  it('reports failure when the dismiss API errors', async () => {
    mocks.postApiSocialFeedDismiss.mockResolvedValue({ ok: false, error: { status: 403 } });

    await expect(dismissPersonalFeedItem('item-1')).resolves.toBe(false);
  });
});
