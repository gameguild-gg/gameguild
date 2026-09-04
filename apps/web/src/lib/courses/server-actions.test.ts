import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  getCoursesSlug: vi.fn(),
  getCoursesById: vi.fn(),
  getCoursesMeProgress: vi.fn(),
  getCoursesContent: vi.fn(),
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
      getCoursesById = mocks.getCoursesById;
      getCoursesMeProgress = mocks.getCoursesMeProgress;
    },
    LearningCoursesProgramContentModule: class {
      getCoursesContent = mocks.getCoursesContent;
      postCoursesContentSubmit = mocks.postCoursesContentSubmit;
    },
  },
}));

import { createTrueFalseEntry } from '@game-guild/quiz';
import {
  enableQuizContentGrading,
  quizContentItemsToDocument,
} from '@game-guild/quiz-content';
import { getCourseLearningData, submitActivity } from './server-actions';

describe('course server actions', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({});
    mocks.getCoursesSlug.mockReset();
    mocks.getCoursesById.mockReset();
    mocks.getCoursesMeProgress.mockReset();
    mocks.getCoursesContent.mockReset();
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

  it('does not route graded quiz submissions through generic content completion', async () => {
    const result = await submitActivity({
      activityId: '3d85ccca-7428-4fc9-88c7-13670a98d0f1',
      courseId: 'course-1',
      activityType: 'quiz',
      content: {},
      isGraded: true,
      attempt: 1,
    });

    expect(result).toEqual({
      success: false,
      message: 'Official graded submissions are not available through the generic activity endpoint.',
    });
    expect(mocks.postCoursesContentSubmit).not.toHaveBeenCalled();
  });

  it('redacts answer keys before returning a server-graded quiz to learners', async () => {
    const question = createTrueFalseEntry('Server-owned answer');
    const jsonBody = enableQuizContentGrading(
      quizContentItemsToDocument({
        items: [{ id: 'question-1', entry: question }],
      }),
    );

    mocks.getCoursesSlug.mockResolvedValue({
      ok: true,
      data: { id: 'course-1', title: 'Course', description: '' },
    });
    mocks.getCoursesById.mockResolvedValue({
      ok: true,
      data: { id: 'course-1', title: 'Course' },
    });
    mocks.getCoursesContent.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'quiz-1',
          title: 'Quiz',
          type: 'Questionnaire',
          sortOrder: 1,
          jsonBody,
        },
      ],
    });
    mocks.getCoursesMeProgress.mockResolvedValue({
      ok: true,
      data: { contentProgress: [], completionPercentage: 0 },
    });

    const course = await getCourseLearningData('course-slug');
    const runtime = course?.currentItem?.content as {
      mode: string;
      document: { blocks: Record<string, Record<string, unknown>> };
    };

    expect(runtime.mode).toBe('server-graded');
    expect(runtime.document.blocks['question-1']).toMatchObject({
      type: 'TRUE_FALSE',
      stem: 'Server-owned answer',
    });
    expect(runtime.document.blocks['question-1']).not.toHaveProperty('correctAnswer');
  });
});
