import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { CourseCard, CourseTableActions } from './course-card';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

const course = {
  id: 'course-123',
  title: 'Combat Design Foundations',
  status: 'draft',
  visibility: 'public',
  enrolledCount: 12,
  completionPercent: 42,
  avgRating: '4.7',
};

describe('CourseCard actions', () => {
  it('exposes accessible edit and preview links from the grid card menu', async () => {
    render(<CourseCard course={course} locale="en-US" />);

    await userEvent.click(screen.getByRole('button', { name: /open combat design foundations actions/i }));

    const menu = screen.getByRole('menu');
    expect(within(menu).getByRole('menuitem', { name: /edit course/i })).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/course-123',
    );
    expect(within(menu).getByRole('menuitem', { name: /^preview$/i })).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/course-123/preview',
    );
  });

  it('exposes accessible edit and preview links from the table row menu', async () => {
    render(<CourseTableActions courseId="course-123" courseTitle="Combat Design Foundations" locale="en-US" />);

    await userEvent.click(screen.getByRole('button', { name: /open combat design foundations actions/i }));

    const menu = screen.getByRole('menu');
    expect(within(menu).getByRole('menuitem', { name: /^edit$/i })).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/course-123',
    );
    expect(within(menu).getByRole('menuitem', { name: /^preview$/i })).toHaveAttribute(
      'href',
      '/dashboard/learning/courses/course-123/preview',
    );
  });
});
