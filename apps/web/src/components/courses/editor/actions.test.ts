import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  getCoursesSlug: vi.fn(),
  putCourses: vi.fn(),
  postCourses: vi.fn(),
  postCoursesPublish: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningCoursesProgramModule: class {
      getCoursesSlug = mocks.getCoursesSlug;
      putCourses = mocks.putCourses;
      postCourses = mocks.postCourses;
    },
    LearningCoursesProgramlifecycleModule: class {
      postCoursesPublish = mocks.postCoursesPublish;
    },
  },
}));

import { getCourseBySlug, publishCourse, saveCourse } from './actions';

describe('course editor actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({});
  });

  it('loads a course by slug through the generated Learning Courses API', async () => {
    mocks.getCoursesSlug.mockResolvedValue({
      ok: true,
      data: {
        id: 'course-1',
        title: 'Intro to Game Design',
        slug: 'intro-to-game-design',
        description: 'Build a small playable prototype.',
        category: 'Design',
        difficulty: 'Intermediate',
        status: 'Draft',
        visibility: 'Private',
        thumbnail: 'https://example.test/thumb.png',
        createdAt: '2026-01-01T00:00:00.000Z',
        updatedAt: '2026-01-02T00:00:00.000Z',
      },
    });

    const result = await getCourseBySlug(' Intro-To-Game-Design ');

    expect(mocks.getCoursesSlug).toHaveBeenCalledWith('intro-to-game-design');
    expect(result).toMatchObject({
      id: 'course-1',
      title: 'Intro to Game Design',
      slug: 'intro-to-game-design',
      area: 'Design',
      level: 'Intermediate',
      status: 'Draft',
      isPublic: false,
    });
  });

  it('saves editor changes through PUT /v1/courses/{id}', async () => {
    mocks.putCourses.mockResolvedValue({
      ok: true,
      data: {
        id: 'course-1',
        title: 'Updated Course',
        slug: 'updated-course',
        description: 'Updated description.',
      },
    });

    const result = await saveCourse({
      id: 'course-1',
      title: 'Updated Course',
      slug: 'updated-course',
      description: 'Updated description.',
      level: 'Advanced',
      area: 'Programming',
      status: 'draft',
      isPublic: false,
      isFeatured: true,
      tags: ['unity', 'csharp'],
      tools: ['Unity'],
    });

    expect(result).toBe(true);
    expect(mocks.putCourses).toHaveBeenCalledWith('course-1', {
      title: 'Updated Course',
      slug: 'updated-course',
      description: 'Updated description.',
      difficulty: 'Advanced',
      category: 'Programming',
      visibility: 'Private',
      skillsProvided: 'unity, csharp',
      skillsRequired: 'Unity',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1');
  });

  it('publishes through the generated lifecycle API', async () => {
    mocks.postCoursesPublish.mockResolvedValue({
      ok: true,
      data: { id: 'course-1' },
    });

    const result = await publishCourse('course-1');

    expect(result).toBe(true);
    expect(mocks.postCoursesPublish).toHaveBeenCalledWith('course-1');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses');
  });
});
