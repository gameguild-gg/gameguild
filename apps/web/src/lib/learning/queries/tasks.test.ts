import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  getCourses: vi.fn(),
  getTasks: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesForGetCourses = mocks.getCourses;
    },
    LearningAssessmentsTasksModule: class {
      getMeTasks = mocks.getTasks;
    },
  },
}));

import { getMyTasks, sumAwaitingGrading, type LearningTask } from './tasks';

describe('cross-course learning tasks', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', 'https://api.gameguild.test');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public-api.gameguild.test');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
    mocks.getCourses.mockResolvedValue({ ok: true, data: [] });
  });

  afterEach(() => vi.unstubAllEnvs());

  it('maps supported task types and resolves available course slugs', async () => {
    mocks.getCourses.mockResolvedValue({
      ok: true,
      data: [
        { id: 'course-1', slug: 'game-ai' },
        { id: 'course-2', slug: null },
        { id: null, slug: 'ignored' },
      ],
    });
    mocks.getTasks.mockResolvedValue({
      ok: true,
      data: {
        items: [
          {
            type: 'grade', courseId: 'course-1', courseTitle: 'Game AI', assessmentId: 'a-1',
            assessmentTitle: 'Behavior tree', dueAt: '2026-09-20T00:00:00Z', countSubmitted: 3,
            reviewsCompleted: 1, reviewsRequired: 2,
          },
          { type: 'do', courseId: 'course-2' },
          { type: 'review', courseId: null, courseTitle: null, assessmentId: null, assessmentTitle: null },
          { type: 'unsupported', courseId: 'course-1' },
          { type: null, courseId: 'course-1' },
        ],
      },
    });

    const result = await getMyTasks();

    expect(result).toEqual({
      ok: true,
      tasks: [
        {
          type: 'grade', courseId: 'course-1', courseTitle: 'Game AI', courseSlug: 'game-ai',
          assessmentId: 'a-1', assessmentTitle: 'Behavior tree', dueAt: '2026-09-20T00:00:00Z',
          countSubmitted: 3, reviewsCompleted: 1, reviewsRequired: 2,
        },
        {
          type: 'do', courseId: 'course-2', courseTitle: 'Untitled course', courseSlug: undefined,
          assessmentId: '', assessmentTitle: 'Untitled assessment', dueAt: null,
          countSubmitted: null, reviewsCompleted: null, reviewsRequired: null,
        },
        {
          type: 'review', courseId: '', courseTitle: 'Untitled course', courseSlug: undefined,
          assessmentId: '', assessmentTitle: 'Untitled assessment', dueAt: null,
          countSubmitted: null, reviewsCompleted: null, reviewsRequired: null,
        },
      ],
    });
    expect(mocks.getTasks).toHaveBeenCalledOnce();
    expect(mocks.getCourses).toHaveBeenCalledWith({ take: 100 });
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'https://api.gameguild.test',
      auth: { getAccessToken: expect.any(Function) },
    });
    await expect(mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken()).resolves.toBe('access-token');
  });

  it('continues without slugs when course lookup fails or throws', async () => {
    mocks.getTasks.mockResolvedValue({ ok: true, data: { items: [{ type: 'do', courseId: 'course-1' }] } });
    mocks.getCourses.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getMyTasks()).resolves.toEqual({
      ok: true,
      tasks: [expect.objectContaining({ type: 'do', courseSlug: undefined })],
    });

    mocks.getCourses.mockRejectedValueOnce(new Error('course API unavailable'));
    await expect(getMyTasks()).resolves.toEqual({
      ok: true,
      tasks: [expect.objectContaining({ type: 'do', courseSlug: undefined })],
    });
  });

  it('normalizes an absent task collection', async () => {
    mocks.getTasks.mockResolvedValue({ ok: true, data: { items: null } });
    await expect(getMyTasks()).resolves.toEqual({ ok: true, tasks: [] });
  });

  it.each([
    [{ ok: false, error: { message: 'ignored' } }],
    [new Error('network down')],
  ])('returns a stable task failure', async (result) => {
    if (result instanceof Error) mocks.getTasks.mockRejectedValue(result);
    else mocks.getTasks.mockResolvedValue(result);
    await expect(getMyTasks()).resolves.toEqual({
      ok: false,
      error: 'Failed to load tasks. Please try again.',
    });
  });

  it('uses public and local API URL fallbacks', async () => {
    mocks.getTasks.mockResolvedValue({ ok: true, data: { items: [] } });
    vi.stubEnv('API_URL', '');
    await getMyTasks();
    expect(mocks.createServerClient).toHaveBeenCalledWith(expect.objectContaining({
      baseUrl: 'https://public-api.gameguild.test',
    }));

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await getMyTasks();
    expect(mocks.createServerClient).toHaveBeenCalledWith(expect.objectContaining({
      baseUrl: 'http://localhost:8080',
    }));
  });

  it('sums only grade tasks and treats absent counts as zero', () => {
    const base = {
      courseId: 'course-1', courseTitle: 'Game AI', assessmentId: 'a-1', assessmentTitle: 'Assessment',
      dueAt: null, reviewsCompleted: null, reviewsRequired: null,
    };
    expect(sumAwaitingGrading([])).toBeNull();
    expect(sumAwaitingGrading([
      { ...base, type: 'do', countSubmitted: 99 },
      { ...base, type: 'grade', countSubmitted: 4 },
      { ...base, type: 'grade', countSubmitted: null },
    ] satisfies LearningTask[])).toBe(4);
  });
});
