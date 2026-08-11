import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/i18n/navigation', () => ({
  usePathname: () => '/learn/courses/game-ai/content',
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

const { CourseLearnerNav } = await import('./course-learner-nav');

describe('CourseLearnerNav', () => {
  it('uses native App Router learner paths for activities and grades', () => {
    render(<CourseLearnerNav slug="game-ai" />);

    expect(screen.getByRole('link', { name: 'Content' })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(screen.getByRole('link', { name: 'Activities' })).toHaveAttribute(
      'href',
      '/learn/courses/game-ai/activities',
    );
    expect(screen.getByRole('link', { name: 'Grades' })).toHaveAttribute(
      'href',
      '/learn/courses/game-ai/grades',
    );
  });
});
