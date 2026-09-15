import { render, screen } from '@testing-library/react';
import { createElement, type ComponentProps, type ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import type { CourseAttendanceData } from '@/lib/learner/courses';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...props }: ComponentProps<'a'> & { children: ReactNode }) => <a href={String(href)} {...props}>{children}</a>,
}));
vi.mock('next/image', () => ({
  default: (props: ComponentProps<'img'> & { fill?: boolean }) => {
    const { fill, ...imageProps } = props;
    void fill;
    return createElement('img', { alt: '', ...imageProps });
  },
}));

import { MyCoursesList } from './my-courses-list';

describe('MyCoursesList', () => {
  it('renders the authenticated empty state', () => {
    render(<MyCoursesList courses={[]} />);
    expect(screen.getByRole('heading', { name: 'No active courses' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Browse the course catalog' })).toHaveAttribute('href', '/courses');
  });

  it('renders enrolled courses with progress, links, image fallbacks, and current lesson context', () => {
    const courses = [
      {
        id: 'course-1', slug: 'game-ai', title: 'Game AI', description: 'Course description',
        thumbnail: '/cover.png', overallProgress: 45, remainingMinutes: 61,
        currentItem: { title: 'Behavior trees' },
      },
      {
        id: 'course-2', slug: 'game-design', title: 'Game Design', description: 'Design fundamentals',
        thumbnail: null, overallProgress: 0, remainingMinutes: 0, currentItem: null,
      },
    ] as unknown as CourseAttendanceData[];

    render(<MyCoursesList courses={courses} />);
    expect(screen.getByRole('link', { name: /Game AI/ })).toHaveAttribute('href', '/learn/courses/game-ai');
    expect(screen.getByRole('presentation')).toHaveAttribute('src', '/cover.png');
    expect(screen.getByText('Behavior trees')).toBeInTheDocument();
    expect(screen.getByText('Design fundamentals')).toBeInTheDocument();
    expect(screen.getByText('45% complete')).toBeInTheDocument();
    expect(screen.getByText('2h remaining')).toBeInTheDocument();
    expect(screen.getByText('0h remaining')).toBeInTheDocument();
  });
});
