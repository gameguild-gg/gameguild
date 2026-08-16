import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  getCoursesSlug: vi.fn(),
  postCoursesContentSubmit: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesSlug = mocks.getCoursesSlug;
    },
    LearningCoursesProgramContentModule: class {
      postCoursesContentSubmit = mocks.postCoursesContentSubmit;
    },
  },
}));

import { submitActivity } from './server-actions';

describe('course server actions', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({});
  });

  it('rejects submissions that are not backed by API course content', async () => {
    const result = await submitActivity({
      activityId: 'mock-activity',
      courseId: 'course-1',
      activityType: 'text',
      content: { response: 'I completed the setup activity.' },
      isGraded: false,
      attempt: 1,
    });

    expect(result).toEqual({
      success: false,
      message: 'This activity is not backed by publishable course content yet.',
    });
    expect(mocks.postCoursesContentSubmit).not.toHaveBeenCalled();
  });
});
