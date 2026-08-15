import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getLearningMeSearch: vi.fn(),
  createServerClient: vi.fn(() => ({ client: true })),
  getToken: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningWorkspacesLearnerWorkspaceModule: class {
      getLearningMeSearch = mocks.getLearningMeSearch;
    },
  },
}));

const { searchLearnerWorkspace } = await import('./search-actions');

describe('searchLearnerWorkspace', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('does not call the API until the query has at least two characters', async () => {
    await expect(searchLearnerWorkspace(' a ')).resolves.toEqual({ success: true, items: [] });
    expect(mocks.getLearningMeSearch).not.toHaveBeenCalled();
  });

  it('returns only safe, navigable results from the permission-filtered API', async () => {
    mocks.getLearningMeSearch.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'course-1',
          kind: 'Course',
          title: 'Game AI',
          description: 'Advanced agents',
          route: '/learn/courses/game-ai',
        },
        {
          id: 'unsafe',
          kind: 'Lesson',
          title: 'External redirect',
          route: '//malicious.example',
        },
        { id: 'incomplete', title: 'No route' },
      ],
    });

    await expect(searchLearnerWorkspace(' game ')).resolves.toEqual({
      success: true,
      items: [
        {
          id: 'course-1',
          kind: 'Course',
          title: 'Game AI',
          description: 'Advanced agents',
          route: '/learn/courses/game-ai',
        },
      ],
    });
    expect(mocks.getLearningMeSearch).toHaveBeenCalledWith({ q: 'game', take: 12 });
  });

  it('surfaces API failures as a recoverable search state', async () => {
    mocks.getLearningMeSearch.mockResolvedValue({
      ok: false,
      error: { detail: 'Search service unavailable' },
    });

    await expect(searchLearnerWorkspace('design')).resolves.toEqual({
      success: false,
      error: 'Search service unavailable',
    });
  });
});
