import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import CoursesPage from './page';

const authMock = vi.fn();
const getTokenMock = vi.fn();
const getCoursesMock = vi.fn();

vi.mock('@/auth', () => ({
  auth: () => authMock(),
  getToken: () => getTokenMock(),
}));

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

  beforeEach(() => {
    authMock.mockReset();
    authMock.mockResolvedValue(null);
    getTokenMock.mockReset();
    getTokenMock.mockResolvedValue(null);
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

  it('lists the full catalog for an elevated admin session instead of creator-only courses', async () => {
    const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    const encode = (value: object) => Buffer.from(JSON.stringify(value), 'utf8').toString('base64url');
    const accessToken = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({ sub: 'user-1', [roleClaim]: 'SystemAdmin' })}.${encode({ sig: 'x' })}`;
    authMock.mockResolvedValue({ user: { id: 'user-1' } });
    getTokenMock.mockResolvedValue(accessToken);
    getCoursesMock.mockResolvedValue({ courses: [ownCourse, seededCourse], error: null });

    render(await CoursesPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByText('My Own Course')).toBeInTheDocument();
    expect(screen.getByText('Seeded Catalog Course')).toBeInTheDocument();
  });

  it('keeps creator-only filtering for a regular session', async () => {
    const encode = (value: object) => Buffer.from(JSON.stringify(value), 'utf8').toString('base64url');
    const accessToken = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode({ sub: 'user-1', role: 'Member' })}.${encode({ sig: 'x' })}`;
    authMock.mockResolvedValue({ user: { id: 'user-1' } });
    getTokenMock.mockResolvedValue(accessToken);
    getCoursesMock.mockResolvedValue({ courses: [ownCourse, seededCourse], error: null });

    render(await CoursesPage({ params: Promise.resolve({ locale: 'en-US' }) } as never));

    expect(screen.getByText('My Own Course')).toBeInTheDocument();
    expect(screen.queryByText('Seeded Catalog Course')).not.toBeInTheDocument();
  });
});
