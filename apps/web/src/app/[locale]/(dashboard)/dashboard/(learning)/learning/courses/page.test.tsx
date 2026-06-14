import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import CoursesPage from './page';

const getCoursesMock = vi.fn();

vi.mock('@/lib/learning', () => ({
  getCourses: () => getCoursesMock(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...rest }: { children: ReactNode; href: string }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

describe('dashboard learning courses page', () => {
  beforeEach(() => {
    getCoursesMock.mockReset();
  });

  it('shows an API recovery state instead of an empty library when courses fail to load', async () => {
    getCoursesMock.mockResolvedValue({
      courses: [],
      error: 'Request failed with status 500',
    });

    render(await CoursesPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByRole('heading', { name: /courses could not be loaded/i })).toBeInTheDocument();
    expect(screen.getByText(/this is not an empty course library/i)).toBeInTheDocument();
    expect(screen.getByText(/request failed with status 500/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /retry/i })).toHaveAttribute('href', '/dashboard/learning/courses');
    expect(screen.queryByText(/no courses yet/i)).not.toBeInTheDocument();
  });

  it('shows the empty state only when the API succeeds with zero courses', async () => {
    getCoursesMock.mockResolvedValue({
      courses: [],
      error: null,
    });

    render(await CoursesPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByText(/no courses yet/i)).toBeInTheDocument();
    expect(screen.getByText(/create your first course to start teaching/i)).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: /courses could not be loaded/i })).not.toBeInTheDocument();
  });
});
