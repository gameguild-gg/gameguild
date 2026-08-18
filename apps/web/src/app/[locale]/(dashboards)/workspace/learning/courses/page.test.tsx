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
  const ownCourse = {
    id: 'course-own',
    slug: 'own-course',
    creatorId: 'user-1',
    creatorHandle: 'user-1',
    routeParam: 'own-course',
    title: 'My Own Course',
    thumbnail: null,
    status: 'published' as const,
    visibility: 'public' as const,
    enrolledCount: 3,
    completionPercent: null,
    avgRating: null,
  };
  const seededCourse = {
    id: 'course-seeded',
    slug: 'seeded-course',
    creatorId: 'seeder-user',
    creatorHandle: 'seeder-user',
    routeParam: 'seeded-course',
    title: 'Seeded Catalog Course',
    thumbnail: null,
    status: 'draft' as const,
    visibility: 'private' as const,
    enrolledCount: 0,
    completionPercent: null,
    avgRating: null,
  };
  const adminCourse = {
    id: 'course-admin',
    slug: 'admin-course',
    creatorId: 'admin-user',
    creatorHandle: 'admin-user',
    routeParam: 'admin-course',
    title: 'Admin Created Course',
    thumbnail: null,
    status: 'published' as const,
    visibility: 'public' as const,
    enrolledCount: 7,
    completionPercent: null,
    avgRating: null,
  };

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
    expect(screen.getByRole('link', { name: /retry/i })).toHaveAttribute('href', '/workspace/learning/courses');
    expect(screen.queryByText(/no courses yet/i)).not.toBeInTheDocument();
  });

  it('shows the empty state only when the API succeeds with zero courses', async () => {
    getCoursesMock.mockResolvedValue({
      courses: [],
      error: null,
    });

    render(await CoursesPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByText(/no courses in the live catalog/i)).toBeInTheDocument();
    expect(screen.getByText(/the public storefront reads from the same course source/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /open storefront/i })).toHaveAttribute('href', '/courses');
    expect(screen.queryByRole('heading', { name: /courses could not be loaded/i })).not.toBeInTheDocument();
  });

  it('renders exactly the courses the API returns, regardless of creator', async () => {
    getCoursesMock.mockResolvedValue({
      courses: [ownCourse, seededCourse, adminCourse],
      error: null,
    });

    render(await CoursesPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByText('My Own Course')).toBeInTheDocument();
    expect(screen.getByText('Seeded Catalog Course')).toBeInTheDocument();
    expect(screen.getByText('Admin Created Course')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });
});
