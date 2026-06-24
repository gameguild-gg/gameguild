import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BrowseOwnedCoursesPage, type EnrolledCourse } from './browse-owned-courses';

const push = vi.fn();

vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ push }),
}));

describe('BrowseOwnedCoursesPage', () => {
  const enrolledCourses: EnrolledCourse[] = [
    {
      id: 'course-1',
      title: 'Game Development Fundamentals',
      description: 'Learn the core concepts of game development.',
      instructor: 'Sarah Johnson',
      thumbnail: '/images/courses/game-dev-fundamentals.jpg',
      progress: 75,
      totalLessons: 20,
      completedLessons: 15,
      estimatedTime: 3,
      difficulty: 'Beginner',
      category: 'Game Development',
      enrolledAt: '2024-01-15',
      lastAccessed: '2024-01-20',
      status: 'in-progress',
      rating: 4.8,
      nextLesson: {
        id: 'lesson-16',
        title: 'Advanced Game Mechanics',
      },
    },
    {
      id: 'course-2',
      title: 'Unity 3D Essentials',
      description: 'Master Unity 3D engine from basics to advanced features.',
      instructor: 'Mike Chen',
      thumbnail: '/images/courses/unity-3d.jpg',
      progress: 100,
      totalLessons: 25,
      completedLessons: 25,
      estimatedTime: 0,
      difficulty: 'Intermediate',
      category: 'Game Engines',
      enrolledAt: '2023-12-01',
      lastAccessed: '2024-01-18',
      status: 'completed',
      certificateEarned: true,
      rating: 4.9,
    },
  ];

  beforeEach(() => {
    push.mockClear();
  });

  it('navigates learner actions to content, certificate, and catalog routes', async () => {
    render(<BrowseOwnedCoursesPage courses={enrolledCourses} />);

    await userEvent.click(screen.getAllByRole('button', { name: /continue/i })[0]);
    expect(push).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/content/lesson-16');

    await userEvent.click(screen.getByRole('button', { name: /review course/i }));
    expect(push).toHaveBeenCalledWith('/dashboard/learning/courses/course-2/content');

    await userEvent.click(screen.getByRole('button', { name: /open unity 3d essentials menu/i }));
    await userEvent.click(screen.getByRole('menuitem', { name: /view certificate/i }));
    expect(push).toHaveBeenCalledWith('/dashboard/learning/courses/course-2/certificates');
  });

  it('does not render seeded courses when no live data is provided', async () => {
    render(<BrowseOwnedCoursesPage />);

    expect(screen.getByText(/you haven't enrolled in any courses yet/i)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: /browse course catalog/i }));
    expect(push).toHaveBeenCalledWith('/courses');
  });
});
