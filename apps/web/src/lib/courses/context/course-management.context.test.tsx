import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCourses: vi.fn(),
}));

vi.mock('@/lib/courses/services/course.service', () => ({
  courseService: {
    getCourses: mocks.getCourses,
  },
}));

import { CoursesSyncProvider, useCoursesSync } from './course-management.context';

function Harness() {
  const { state, getEnhancedCourses, syncWithServer } = useCoursesSync();
  const courses = getEnhancedCourses();

  return (
    <div>
      <span data-testid="status">{state.syncStatus}</span>
      <span data-testid="count">{courses.length}</span>
      <span data-testid="first">{courses[0]?.title ?? 'none'}</span>
      <button type="button" onClick={() => void syncWithServer()}>
        Sync
      </button>
    </div>
  );
}

describe('CoursesSyncProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourses.mockResolvedValue([
      {
        id: 'course-1',
        title: 'Gameplay Systems',
        slug: 'gameplay-systems',
        description: 'Build game loop systems.',
        category: 'Programming',
        difficulty: 'Intermediate',
        thumbnail: '/courses/gameplay.png',
      },
    ]);
  });

  it('syncs courses from the live course service and exposes enhanced courses', async () => {
    render(
      <CoursesSyncProvider>
        <Harness />
      </CoursesSyncProvider>,
    );

    await userEvent.click(screen.getByRole('button', { name: 'Sync' }));

    await waitFor(() => expect(screen.getByTestId('status')).toHaveTextContent('synced'));
    expect(screen.getByTestId('count')).toHaveTextContent('1');
    expect(screen.getByTestId('first')).toHaveTextContent('Gameplay Systems');
    expect(mocks.getCourses).toHaveBeenCalledTimes(1);
  });
});
