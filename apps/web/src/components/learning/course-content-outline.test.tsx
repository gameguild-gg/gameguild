import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

import type { CourseAttendanceData } from '@/lib/learner/courses';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

const { CourseContentOutline } = await import('./course-content-outline');

const course: CourseAttendanceData = {
  id: 'course-1',
  title: 'Game AI',
  slug: 'game-ai',
  description: 'Build game intelligence.',
  thumbnail: null,
  modules: [
    {
      id: 'module-1',
      title: 'Foundations',
      description: '',
      order: 0,
      progress: 33,
      items: [
        {
          id: 'lesson-1',
          title: 'Introduction',
          type: 'lesson',
          status: 'available',
          order: 0,
          isRequired: true,
        },
        {
          id: 'discussion-1',
          title: 'Discuss game loops',
          type: 'activity',
          contentType: 'Discussion',
          status: 'in-progress',
          order: 1,
          isRequired: true,
        },
        {
          id: 'lesson-2',
          title: 'Advanced agents',
          type: 'lesson',
          status: 'locked',
          order: 2,
          isRequired: true,
        },
      ],
    },
  ],
  overallProgress: 33,
  totalItems: 3,
  completedItems: 1,
  remainingMinutes: 90,
};

describe('CourseContentOutline', () => {
  it('uses native App Router paths for lessons and activities', () => {
    render(<CourseContentOutline course={course} />);

    expect(screen.getByRole('link', { name: /Introduction/ })).toHaveAttribute(
      'href',
      '/learn/courses/game-ai/lessons/lesson-1',
    );
    expect(screen.getByRole('link', { name: /Discuss game loops/ })).toHaveAttribute(
      'href',
      '/learn/courses/game-ai/activities/content-discussion-1',
    );
  });

  it('does not expose a navigation link for locked content', () => {
    render(<CourseContentOutline course={course} />);

    expect(screen.getByText('Advanced agents')).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /Advanced agents/ })).not.toBeInTheDocument();
  });
});
