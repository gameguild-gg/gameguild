import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getCourses: vi.fn(),
  getContent: vi.fn(),
}));

vi.mock('react', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react')>()),
  cache: <T extends (...args: never[]) => unknown>(callback: T) => callback,
}));
vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesForGetCourses = mocks.getCourses;
    },
    LearningCoursesProgramContentModule: class {
      getCoursesContent = mocks.getContent;
    },
  },
}));

import { getLearningContentLibrary } from './content-library';

describe('learning content library query', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', 'https://api.gameguild.test');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public-api.gameguild.test');
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-15T12:00:00.000Z'));
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllEnvs();
  });

  it('flattens, normalizes, and sorts content from every valid course', async () => {
    mocks.getCourses.mockResolvedValue({
      ok: true,
      data: [
        { id: null, title: 'Ignored' },
        { id: 'course-1', title: 'Game AI', slug: 'game-ai', updatedAt: '2026-01-01T00:00:00Z' },
        { id: 'course-2', title: null, slug: null, updatedAt: '2026-02-01T00:00:00Z' },
      ],
    });
    mocks.getContent.mockImplementation(async (courseId: string) => {
      if (courseId === 'course-2') return { ok: false, error: {} };
      return {
        ok: true,
        data: [
          {
            id: 'module-1', slug: 'module', title: 'Module', type: 'Module', visibility: 'Private',
            sortOrder: 2, updatedAt: '2026-09-10T00:00:00Z', children: [
              {
                id: 'lesson-1', slug: 'lesson', title: 'Lesson', description: 'Learn.', type: 'Page',
                visibility: 'PUBLIC', sortOrder: 1, estimatedMinutes: 15, isRequired: true,
                updatedAt: '2026-09-14T00:00:00Z', children: null,
              },
            ],
          },
          {
            id: 'challenge-1', slug: null, title: null, description: null, type: 'Challenge',
            visibility: null, sortOrder: null, estimatedMinutes: null, isRequired: null,
            updatedAt: null, createdAt: '2026-09-12T00:00:00Z',
          },
          { id: null, title: 'Ignored content' },
        ],
      };
    });

    const result = await getLearningContentLibrary();

    expect(mocks.getCourses).toHaveBeenCalledWith({ take: 100 });
    expect(mocks.getContent).toHaveBeenCalledTimes(2);
    expect(result.error).toBeNull();
    expect(result.items).toEqual([
      expect.objectContaining({
        id: 'lesson-1', slug: 'lesson', type: 'Lesson', visibility: 'public', status: 'published',
        durationMinutes: 15, isRequired: true, updatedAt: '2026-09-14T00:00:00Z',
      }),
      expect.objectContaining({
        id: 'challenge-1', slug: 'challenge-1', title: 'Untitled content', type: 'Assignment',
        visibility: 'private', status: 'draft', durationMinutes: null, isRequired: false,
        updatedAt: '2026-09-12T00:00:00Z',
      }),
      expect.objectContaining({ id: 'module-1', type: 'Module', visibility: 'private' }),
    ]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'https://api.gameguild.test',
      auth: { getAccessToken: expect.any(Function) },
    });
    await expect(mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken()).resolves.toBe('access-token');
  });

  it('uses all content metadata fallbacks, including the current timestamp', async () => {
    mocks.getCourses.mockResolvedValue({ ok: true, data: [{ id: 42, title: null, slug: null, updatedAt: null }] });
    mocks.getContent.mockResolvedValue({
      ok: true,
      data: [{
        id: 7, slug: null, title: null, description: undefined, type: null, visibility: undefined,
        sortOrder: undefined, estimatedMinutes: undefined, isRequired: undefined,
        updatedAt: undefined, createdAt: undefined, children: undefined,
      }],
    });

    await expect(getLearningContentLibrary()).resolves.toEqual({
      items: [{
        id: '7', slug: '7', courseId: '42', courseTitle: 'Untitled course', courseSlug: '',
        title: 'Untitled content', description: null, type: 'Lesson', visibility: 'private',
        status: 'draft', durationMinutes: null, isRequired: false, updatedAt: '2026-09-15T12:00:00.000Z',
      }],
      error: null,
    });
  });

  it.each([
    [{ status: 403, code: 'FORBIDDEN', detail: 'Not allowed.' }, '[403 FORBIDDEN] Not allowed.'],
    [{ status: 500, message: 'Backend failed.' }, '[500] Backend failed.'],
    [undefined, '[unknown] Failed to load courses'],
  ])('returns useful course API failures', async (error, expected) => {
    mocks.getCourses.mockResolvedValue({ ok: false, error });
    await expect(getLearningContentLibrary()).resolves.toEqual({ items: [], error: expected });
  });

  it.each([
    [new Error('network down'), 'Unexpected: network down'],
    ['offline', 'Unexpected: offline'],
  ])('normalizes unexpected query failures', async (reason, expected) => {
    mocks.getCourses.mockRejectedValue(reason);
    await expect(getLearningContentLibrary()).resolves.toEqual({ items: [], error: expected });
  });

  it('uses public and local API URL fallbacks', async () => {
    mocks.getCourses.mockResolvedValue({ ok: true, data: [] });
    vi.stubEnv('API_URL', '');
    await getLearningContentLibrary();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({
      baseUrl: 'https://public-api.gameguild.test',
    }));

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await getLearningContentLibrary();
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({
      baseUrl: 'http://localhost:8080',
    }));
  });
});
