import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCourseAccessData: vi.fn(),
  canEditCourse: vi.fn(),
}));

vi.mock('@/lib/learner/courses', () => ({
  getCourseAccessData: mocks.getCourseAccessData,
}));
vi.mock('@/lib/learning/queries/course', () => ({
  canEditCourse: mocks.canEditCourse,
}));
vi.mock('@/components/learning/course-access-gate', () => ({
  CourseAccessGate: () => <div data-testid="access-gate" />,
}));
vi.mock('@/components/learning/learner-lesson-renderer', () => ({
  LearnerLessonRenderer: () => <div data-testid="lesson-renderer" />,
}));
vi.mock('@/components/learning/lesson-progress-controls', () => ({
  LessonProgressControls: () => <div data-testid="progress-controls" />,
}));
vi.mock('next/navigation', () => ({
  notFound: () => {
    throw new Error('not-found');
  },
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href }: { children: ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

import LessonPage from './page';

function makeReadyAccess(items: Array<Record<string, unknown>>) {
  return {
    kind: 'ready' as const,
    course: {
      id: 'course-1',
      title: 'Test Course',
      slug: 'test-course',
      description: '',
      thumbnail: null,
      modules: [{ id: 'module-1', title: 'Module 1', description: '', order: 0, items, progress: 0 }],
      overallProgress: 0,
      totalItems: items.length,
      completedItems: 0,
      remainingMinutes: 0,
      enrollmentId: 'enrollment-1',
    },
  };
}

function makeLesson(overrides: Record<string, unknown> = {}) {
  return {
    id: 'content-1',
    slug: 'setup',
    title: 'Setup lesson',
    description: 'Get ready',
    type: 'lesson',
    status: 'available',
    duration: 10,
    order: 0,
    isRequired: false,
    ...overrides,
  };
}

async function renderLessonPage(lessonSlug = 'setup') {
  const page = await LessonPage({
    params: Promise.resolve({ lessonSlug, slug: 'test-course' }),
  });
  return render(page);
}

describe('lesson page edit shortcut', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourseAccessData.mockResolvedValue(
      makeReadyAccess([makeLesson()]),
    );
  });

  it('renders the pen link to the console editor when the viewer can edit', async () => {
    mocks.canEditCourse.mockResolvedValue(true);

    await renderLessonPage();

    const editLink = screen.getByRole('link', { name: 'Edit lesson' });
    expect(editLink).toHaveAttribute(
      'href',
      '/console/learning/courses/test-course/content/setup',
    );
    expect(mocks.canEditCourse).toHaveBeenCalledWith('course-1');
  });

  it('hides the pen link when the viewer cannot edit', async () => {
    mocks.canEditCourse.mockResolvedValue(false);

    await renderLessonPage();

    expect(screen.queryByRole('link', { name: 'Edit lesson' })).not.toBeInTheDocument();
  });

  it('falls back to the content id when the lesson has no slug', async () => {
    mocks.canEditCourse.mockResolvedValue(true);
    mocks.getCourseAccessData.mockResolvedValue(
      makeReadyAccess([makeLesson({ slug: undefined, id: 'guid-1' })]),
    );

    await renderLessonPage('guid-1');

    expect(screen.getByRole('link', { name: 'Edit lesson' })).toHaveAttribute(
      'href',
      '/console/learning/courses/test-course/content/guid-1',
    );
  });
});
