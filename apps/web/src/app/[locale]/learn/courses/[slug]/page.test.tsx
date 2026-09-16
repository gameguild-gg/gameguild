import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  createLearnerRoutes: vi.fn(),
  getCourseAccessData: vi.fn(),
  getCourseGroupSetViews: vi.fn(),
  getCourseLearnerContext: vi.fn(),
  notFound: vi.fn(() => {
    throw new Error('not-found');
  }),
  overviewProps: vi.fn(),
  groupsProps: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/components/learning/course-access-gate', () => ({
  CourseAccessGate: ({ access }: { access: { kind: string } }) => (
    <div data-testid="access-gate">{access.kind}</div>
  ),
}));
vi.mock('@/lib/learner/courses', () => ({
  getCourseAccessData: mocks.getCourseAccessData,
}));
vi.mock('@/lib/learner/records', () => ({
  getCourseLearnerContext: mocks.getCourseLearnerContext,
}));
vi.mock('@/lib/learner/routes', () => ({
  createLearnerRoutes: mocks.createLearnerRoutes,
}));
vi.mock('@/lib/learning', () => ({
  getCourseGroupSetViews: mocks.getCourseGroupSetViews,
}));
vi.mock('@game-guild/courses/components/learner', () => ({
  CourseLearnerOverview: (props: unknown) => {
    mocks.overviewProps(props);
    return <div data-testid="course-overview" />;
  },
}));
vi.mock('next/navigation', () => ({ notFound: mocks.notFound }));
vi.mock('./groups-section', () => ({
  LearnCourseGroups: (props: unknown) => {
    mocks.groupsProps(props);
    return <div data-testid="course-groups" />;
  },
}));

import CourseOverviewPage from './page';

const readyCourse = {
  id: 'course-1',
  slug: 'course-one',
  title: 'Course one',
  modules: [],
};

async function renderPage() {
  const page = await CourseOverviewPage({
    params: Promise.resolve({ locale: 'pt-BR', slug: 'course-one' }),
  });
  return render(page);
}

describe('learner course overview page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue(null);
    mocks.createLearnerRoutes.mockReturnValue({ content: '/course/content' });
    mocks.getCourseLearnerContext.mockResolvedValue({ progress: 0 });
    mocks.getCourseGroupSetViews.mockResolvedValue([{ id: 'set-1' }]);
  });

  it('delegates unavailable courses to the access gate', async () => {
    mocks.getCourseAccessData.mockResolvedValue({ kind: 'purchase-required' });

    await renderPage();

    expect(screen.getByTestId('access-gate')).toHaveTextContent(
      'purchase-required',
    );
    expect(mocks.getCourseGroupSetViews).not.toHaveBeenCalled();
  });

  it('returns not found for an unknown course', async () => {
    mocks.getCourseAccessData.mockResolvedValue({ kind: 'not-found' });

    await expect(renderPage()).rejects.toThrow('not-found');
    expect(mocks.notFound).toHaveBeenCalledOnce();
  });

  it('does not request protected group sets before enrollment', async () => {
    mocks.getCourseAccessData.mockResolvedValue({
      kind: 'ready',
      course: readyCourse,
    });

    await renderPage();

    expect(screen.getByTestId('course-overview')).toBeInTheDocument();
    expect(screen.getByTestId('course-groups')).toBeInTheDocument();
    expect(mocks.getCourseGroupSetViews).not.toHaveBeenCalled();
    expect(mocks.createLearnerRoutes).toHaveBeenCalledWith('pt-BR');
    expect(mocks.groupsProps).toHaveBeenCalledWith({
      courseId: 'course-1',
      currentUserId: '',
      sets: [],
    });
  });

  it('loads group sets for the enrolled authenticated learner', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'learner-1' } });
    mocks.getCourseAccessData.mockResolvedValue({
      kind: 'ready',
      course: { ...readyCourse, enrollmentId: 'enrollment-1' },
    });

    await renderPage();

    expect(mocks.getCourseGroupSetViews).toHaveBeenCalledWith('course-1');
    expect(mocks.getCourseLearnerContext).toHaveBeenCalledWith('course-1');
    expect(mocks.groupsProps).toHaveBeenCalledWith({
      courseId: 'course-1',
      currentUserId: 'learner-1',
      sets: [{ id: 'set-1' }],
    });
  });
});
