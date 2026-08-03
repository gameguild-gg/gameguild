import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const navigation = vi.hoisted(() => ({
  pathname: '/en-US/learn/courses/game-ai/content',
}));

vi.mock('next/navigation', () => ({
  usePathname: () => navigation.pathname,
}));

const { CourseLearnerNav } = await import('./course-learner-nav');

describe('CourseLearnerNav', () => {
  it('marks the visible learner route active when Next renders an internal rewrite pathname', () => {
    render(<CourseLearnerNav initialPathname="/en-US/learn/courses/game-ai/content" slug="game-ai" />);

    expect(screen.getByRole('link', { name: 'Content' })).toHaveAttribute(
      'aria-current',
      'page',
    );
  });
});
